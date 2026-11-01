using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Yvira.Vm;

public class DbExecResult
{
    public long RowsAffected { get; set; }
    public long LastInsertId { get; set; }
    public bool Success { get; set; } = true;
}

public class DatabaseManager : IDisposable
{
    private readonly ConcurrentDictionary<string, DbConnection> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DbTransaction> _activeTransactions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, object> _txLocks = new(StringComparer.OrdinalIgnoreCase);

    private object GetTxLock(string alias)
    {
        return _txLocks.GetOrAdd(alias, _ => new object());
    }

    public void Connect(string alias, string connectionUri, string workingDirectory)
    {
        alias = string.IsNullOrWhiteSpace(alias) ? "default" : alias.Trim().ToLowerInvariant();

        // Fechar conexao anterior se ja existia
        Disconnect(alias);

        DbConnection conn;

        if (connectionUri.StartsWith("sqlite://", StringComparison.OrdinalIgnoreCase))
        {
            string dbPath = connectionUri.Substring("sqlite://".Length).Trim();
            if (dbPath != ":memory:" && !Path.IsPathRooted(dbPath))
            {
                dbPath = Path.Combine(workingDirectory, dbPath);
            }

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            conn = new SqliteConnection(builder.ConnectionString);
        }
        else
        {
            // Padrao caso seja arquivo direto ou sqlite
            string dbPath = connectionUri;
            if (!Path.IsPathRooted(dbPath))
            {
                dbPath = Path.Combine(workingDirectory, dbPath);
            }
            conn = new SqliteConnection($"Data Source={dbPath}");
        }

        conn.Open();
        _connections[alias] = conn;
    }

    public DbConnection GetConnection(string? alias)
    {
        alias = string.IsNullOrWhiteSpace(alias) ? "default" : alias.Trim().ToLowerInvariant();

        if (!_connections.TryGetValue(alias, out var conn))
        {
            throw new Exception($"Nenhuma conexao de banco de dados ativa para o alias '{alias}'. Use @db ou @db.{alias}('...') antes de executar consultas.");
        }

        return conn;
    }

    public DbExecResult ExecuteNonQueryDetailed(string? alias, string sql, IList<object?>? parameters)
    {
        alias = string.IsNullOrWhiteSpace(alias) ? "default" : alias.Trim().ToLowerInvariant();
        var conn = GetConnection(alias);
        lock (conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (_activeTransactions.TryGetValue(alias, out var tx))
            {
                cmd.Transaction = tx;
            }

            BindParameters(cmd, parameters);

            int rows = cmd.ExecuteNonQuery();
            long lastId = 0;

            if (conn is SqliteConnection sConn && sql.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                using var idCmd = sConn.CreateCommand();
                if (tx != null && tx is SqliteTransaction sTx) idCmd.Transaction = sTx;
                idCmd.CommandText = "SELECT last_insert_rowid();";
                var idObj = idCmd.ExecuteScalar();
                if (idObj != null && Convert.ToInt64(idObj) > 0)
                {
                    lastId = Convert.ToInt64(idObj);
                }
            }

            return new DbExecResult
            {
                RowsAffected = rows,
                LastInsertId = lastId,
                Success = true
            };
        }
    }

    public long ExecuteNonQuery(string? alias, string sql, IList<object?>? parameters)
    {
        var res = ExecuteNonQueryDetailed(alias, sql, parameters);
        if (res.LastInsertId > 0 && res.RowsAffected == 1)
        {
            return res.LastInsertId;
        }
        return res.RowsAffected;
    }

    public void BeginTransaction(string? alias = null)
    {
        alias = string.IsNullOrWhiteSpace(alias) ? "default" : alias.Trim().ToLowerInvariant();
        var txLock = GetTxLock(alias);
        System.Threading.Monitor.Enter(txLock);

        try
        {
            var conn = GetConnection(alias);
            lock (conn)
            {
                var tx = conn.BeginTransaction();
                _activeTransactions[alias] = tx;
            }
        }
        catch
        {
            System.Threading.Monitor.Exit(txLock);
            throw;
        }
    }

    public void Commit(string? alias = null)
    {
        alias = string.IsNullOrWhiteSpace(alias) ? "default" : alias.Trim().ToLowerInvariant();
        var txLock = GetTxLock(alias);

        try
        {
            var conn = GetConnection(alias);
            lock (conn)
            {
                if (_activeTransactions.TryRemove(alias, out var tx))
                {
                    tx.Commit();
                    tx.Dispose();
                }
                else
                {
                    throw new Exception($"Nenhuma transação ativa para efetuar commit na conexão '{alias}'.");
                }
            }
        }
        finally
        {
            if (System.Threading.Monitor.IsEntered(txLock))
            {
                System.Threading.Monitor.Exit(txLock);
            }
        }
    }

    public void Rollback(string? alias = null)
    {
        alias = string.IsNullOrWhiteSpace(alias) ? "default" : alias.Trim().ToLowerInvariant();
        var txLock = GetTxLock(alias);

        try
        {
            var conn = GetConnection(alias);
            lock (conn)
            {
                if (_activeTransactions.TryRemove(alias, out var tx))
                {
                    tx.Rollback();
                    tx.Dispose();
                }
            }
        }
        finally
        {
            if (System.Threading.Monitor.IsEntered(txLock))
            {
                System.Threading.Monitor.Exit(txLock);
            }
        }
    }

    public List<object?> ExecuteQuery(string? alias, string sql, IList<object?>? parameters)
    {
        alias = string.IsNullOrWhiteSpace(alias) ? "default" : alias.Trim().ToLowerInvariant();
        var conn = GetConnection(alias);
        lock (conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (_activeTransactions.TryGetValue(alias, out var tx))
            {
                cmd.Transaction = tx;
            }

            BindParameters(cmd, parameters);

            using var reader = cmd.ExecuteReader();
            var results = new List<object?>();

            while (reader.Read())
            {
                var map = new YviraMap();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string colName = reader.GetName(i);
                    object val = reader.GetValue(i);
                    if (val == DBNull.Value)
                    {
                        map.Set(colName, null);
                    }
                    else if (val is long l)
                    {
                        map.Set(colName, (double)l);
                    }
                    else if (val is int intVal)
                    {
                        map.Set(colName, (double)intVal);
                    }
                    else if (val is float f)
                    {
                        map.Set(colName, (double)f);
                    }
                    else
                    {
                        map.Set(colName, val);
                    }
                }
                results.Add(map);
            }

            return results;
        }
    }

    private void BindParameters(DbCommand cmd, IList<object?>? parameters)
    {
        if (parameters == null || parameters.Count == 0) return;

        for (int i = 0; i < parameters.Count; i++)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = $"@p{i}";
            object? rawVal = parameters[i];
            while (rawVal is YviraMatrix mat && mat.Rows == 1 && mat.Cols == 1)
            {
                rawVal = mat.Get(0, 0);
            }
            if (rawVal is YviraMatrix otherMat)
            {
                rawVal = otherMat.Get(0, 0);
            }
            param.Value = rawVal ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }

        // Se a query contiver '?' posicionais, substitui pelo nome do parametro ordenado @p0, @p1...
        if (cmd.CommandText.Contains('?'))
        {
            string newSql = cmd.CommandText;
            int paramIndex = 0;
            while (newSql.Contains('?') && paramIndex < parameters.Count)
            {
                int qPos = newSql.IndexOf('?');
                newSql = newSql.Substring(0, qPos) + $"@p{paramIndex}" + newSql.Substring(qPos + 1);
                paramIndex++;
            }
            cmd.CommandText = newSql;
        }
    }

    public void Disconnect(string alias)
    {
        if (_connections.TryRemove(alias, out var conn))
        {
            try { conn.Close(); conn.Dispose(); } catch { }
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _connections)
        {
            try { kvp.Value.Close(); kvp.Value.Dispose(); } catch { }
        }
        _connections.Clear();
    }
}
