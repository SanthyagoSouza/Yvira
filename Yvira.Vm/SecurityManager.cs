using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Yvira.Vm;

/// <summary>
/// Rate Limiter de alta precisão para requisições HTTP por endereço IP.
/// </summary>
public class HttpRateLimiter
{
    private class IpCounter
    {
        public int Count;
        public long WindowStart;
    }

    private readonly ConcurrentDictionary<string, IpCounter> _counters = new();
    public int MaxRequestsPerMinute { get; set; } = 120; // 120 reqs/minuto default
    public bool Enabled { get; set; } = true;

    public bool IsAllowed(string ip)
    {
        if (!Enabled || MaxRequestsPerMinute <= 0) return true;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var counter = _counters.GetOrAdd(ip, _ => new IpCounter { Count = 0, WindowStart = now });

        lock (counter)
        {
            if (now - counter.WindowStart >= 60)
            {
                counter.Count = 0;
                counter.WindowStart = now;
            }

            counter.Count++;
            return counter.Count <= MaxRequestsPerMinute;
        }
    }
}

/// <summary>
/// Gerenciador de Políticas de Segurança e Sandboxing do Yvira.
/// Controla acesso a Sistema de Arquivos (FS), Rede (Net), Processos e Variáveis de Ambiente.
/// </summary>
public class SecurityPolicy
{
    public bool Enabled { get; set; } = false;

    // Permissões de Arquivo
    public bool AllowFsRead { get; set; } = true;
    public bool AllowFsWrite { get; set; } = true;
    public HashSet<string> AllowedReadPaths { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllowedWritePaths { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Permissões de Rede
    public bool AllowNet { get; set; } = true;
    public HashSet<string> AllowedHosts { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Permissões de Processo / Sistema
    public bool AllowProcessExecution { get; set; } = true;
    public bool AllowEnvironmentRead { get; set; } = true;

    // Taint Tracking (Segurança contra SQL Injection)
    public bool EnforceAntiSqlInjection { get; set; } = true;

    // Proteção HTTP Anti-DoS
    public int HttpMaxPayloadBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public HttpRateLimiter HttpRateLimiter { get; } = new();

    public void AssertFsRead(string path, string workingDir)
    {
        if (!Enabled || AllowFsRead) return;

        string full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(workingDir, path));
        foreach (var allowed in AllowedReadPaths)
        {
            string fullAllowed = Path.GetFullPath(Path.IsPathRooted(allowed) ? allowed : Path.Combine(workingDir, allowed));
            if (full.StartsWith(fullAllowed, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        throw new SecurityException($"[ACESSO NEGADO] Tentativa não autorizada de leitura do arquivo/diretório: '{path}'. A política de sandbox do Yvira bloqueou esta operação.");
    }

    public void AssertFsWrite(string path, string workingDir)
    {
        if (!Enabled || AllowFsWrite) return;

        string full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(workingDir, path));
        foreach (var allowed in AllowedWritePaths)
        {
            string fullAllowed = Path.GetFullPath(Path.IsPathRooted(allowed) ? allowed : Path.Combine(workingDir, allowed));
            if (full.StartsWith(fullAllowed, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        throw new SecurityException($"[ACESSO NEGADO] Tentativa não autorizada de escrita/modificação no arquivo/diretório: '{path}'. A política de sandbox do Yvira bloqueou esta operação.");
    }

    public void AssertNet(string hostOrUri)
    {
        if (!Enabled || AllowNet) return;

        string host = hostOrUri;
        if (Uri.TryCreate(hostOrUri, UriKind.Absolute, out var uri))
        {
            host = uri.Host;
        }

        foreach (var allowed in AllowedHosts)
        {
            if (string.Equals(allowed, host, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(allowed, "*", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        throw new SecurityException($"[ACESSO NEGADO] Tentativa não autorizada de conexão de rede com o host: '{hostOrUri}'. A política de sandbox do Yvira bloqueou esta operação.");
    }

    public void AssertProcessExecution(string command)
    {
        if (!Enabled || AllowProcessExecution) return;
        throw new SecurityException($"[ACESSO NEGADO] Execução de processos externos bloqueada pela política de segurança do Yvira (Comando: '{command}').");
    }

    public void AssertEnvironmentRead(string varName)
    {
        if (!Enabled || AllowEnvironmentRead) return;
        throw new SecurityException($"[ACESSO NEGADO] Leitura de variáveis de ambiente bloqueada pela política de segurança do Yvira (Variável: '{varName}').");
    }

    public SecurityPolicy Clone()
    {
        var clone = new SecurityPolicy
        {
            Enabled = this.Enabled,
            AllowFsRead = this.AllowFsRead,
            AllowFsWrite = this.AllowFsWrite,
            AllowNet = this.AllowNet,
            AllowProcessExecution = this.AllowProcessExecution,
            AllowEnvironmentRead = this.AllowEnvironmentRead,
            EnforceAntiSqlInjection = this.EnforceAntiSqlInjection,
            HttpMaxPayloadBytes = this.HttpMaxPayloadBytes
        };

        clone.HttpRateLimiter.MaxRequestsPerMinute = this.HttpRateLimiter.MaxRequestsPerMinute;
        clone.HttpRateLimiter.Enabled = this.HttpRateLimiter.Enabled;

        foreach (var p in this.AllowedReadPaths) clone.AllowedReadPaths.Add(p);
        foreach (var p in this.AllowedWritePaths) clone.AllowedWritePaths.Add(p);
        foreach (var h in this.AllowedHosts) clone.AllowedHosts.Add(h);

        return clone;
    }
}

public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
}

/// <summary>
/// Envoltório para strings marcadas como NÃO CONFIÁVEIS (Tainted).
/// Provenientes de inputs de rede, argumentos CLI ou queries.
/// Se concatenadas em SQL sem parametrização ?, causam erro de segurança.
/// </summary>
public class TaintedString
{
    public string Value { get; }
    public string Source { get; }

    public TaintedString(string value, string source = "input_externo")
    {
        Value = value;
        Source = source;
    }

    public override string ToString() => Value;

    public static object? WrapIfString(object? obj, string source)
    {
        if (obj is string s)
        {
            return new TaintedString(s, source);
        }
        return obj;
    }

    public static bool IsTainted(object? obj)
    {
        return obj is TaintedString;
    }

    public static string GetRawString(object? obj)
    {
        if (obj is TaintedString ts) return ts.Value;
        return obj?.ToString() ?? "";
    }
}
