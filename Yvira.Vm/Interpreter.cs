using System;
using System.Collections.Generic;
using Yvira.Ast;
using Yvira.Lexer;

namespace Yvira.Vm;

public class Interpreter
{
    private Environment _environment = new();
    private readonly List<string> _runtimeErrors = new();
    private readonly Action<string> _outputHandler;
    private readonly Func<string?> _inputHandler;
    private string _currentDirectory;
    private readonly Dictionary<string, YviraMap> _moduleCache;
    private readonly HashSet<string> _executingModules;
    private readonly Func<string, ProgramNode>? _moduleLoader;
    private static Random _random = new();
    private readonly Scheduler _scheduler = new();
    private readonly DatabaseManager _dbManager = new();
    public DatabaseManager DatabaseManager => _dbManager;
    public Scheduler Scheduler => _scheduler;
    private readonly List<HttpServerInstance> _httpServers = new();
    public IReadOnlyList<HttpServerInstance> HttpServers => _httpServers;

    public void RegisterHttpServer(HttpServerInstance server)
    {
        lock (_httpServers)
        {
            _httpServers.Add(server);
        }
    }

    public void StopAllHttpServers()
    {
        lock (_httpServers)
        {
            foreach (var server in _httpServers)
            {
                server.Dispose();
            }
            _httpServers.Clear();
        }
    }

    public IReadOnlyList<string> RuntimeErrors => _runtimeErrors;
    public Environment Environment => _environment;

    public Interpreter CreateWorkerClone()
    {
        var workerEnv = new Environment(_environment);
        var worker = new Interpreter(
            outputHandler: _outputHandler,
            inputHandler: _inputHandler,
            currentDirectory: _currentDirectory,
            loadedModules: _executingModules,
            moduleLoader: _moduleLoader,
            sharedDbManager: _dbManager,
            initialEnvironment: workerEnv
        );
        return worker;
    }

    public Interpreter(Action<string>? outputHandler = null, Func<string?>? inputHandler = null, string? currentDirectory = null, HashSet<string>? loadedModules = null, Func<string, ProgramNode>? moduleLoader = null, DatabaseManager? sharedDbManager = null, Environment? initialEnvironment = null, IReadOnlyList<string>? scriptArgs = null)
    {
        _outputHandler = outputHandler ?? Console.WriteLine;
        _inputHandler = inputHandler ?? Console.ReadLine;
        _currentDirectory = currentDirectory ?? System.IO.Directory.GetCurrentDirectory();
        _moduleCache = new Dictionary<string, YviraMap>(StringComparer.OrdinalIgnoreCase);
        _executingModules = loadedModules ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _moduleLoader = moduleLoader;
        if (sharedDbManager != null)
        {
            _dbManager = sharedDbManager;
        }
        if (initialEnvironment != null)
        {
            _environment = initialEnvironment;
        }

        // Variável global 'args' disponível para scripts (CLI Arguments)
        if (!_environment.IsDefinedLocally("args"))
        {
            var yArgs = scriptArgs != null ? new YviraList(scriptArgs.Select(a => (object?)a)) : new YviraList();
            _environment.Define("args", yArgs);
        }
    }

    public void SetCurrentDirectory(string dir)
    {
        if (!string.IsNullOrWhiteSpace(dir))
        {
            _currentDirectory = System.IO.Path.GetFullPath(dir);
        }
    }

    public object? Execute(ProgramNode program)
    {
        object? lastResult = null;
        try
        {
            foreach (var statement in program.Statements)
            {
                lastResult = ExecuteStatement(statement);
            }
        }
        catch (Exception ex)
        {
            _runtimeErrors.Add(ex.Message);
        }

        return lastResult;
    }

    public object? ExecuteStatement(StatementNode statement)
    {
        switch (statement)
        {
            case VariableDeclarationNode varDecl:
                ExecuteVariableDeclaration(varDecl);
                return null;

            case ExpressionStatementNode exprStmt:
                return EvaluateExpression(exprStmt.Expression);

            case BlockStatementNode block:
                ExecuteBlock(block);
                return null;

            case IfStatementNode ifStmt:
                ExecuteIfStatement(ifStmt);
                return null;

            case WhileStatementNode whileStmt:
                ExecuteWhileStatement(whileStmt);
                return null;

            case ForStatementNode forStmt:
                ExecuteForStatement(forStmt);
                return null;

            case ForeachStatementNode foreachStmt:
                ExecuteForeachStatement(foreachStmt);
                return null;

            case BreakStatementNode:
                throw new BreakException();

            case ContinueStatementNode:
                throw new ContinueException();

            case FunctionDeclarationNode funcDecl:
                ExecuteFunctionDeclaration(funcDecl);
                return null;

            case ReturnStatementNode retStmt:
                object? retVal = retStmt.Value != null ? EvaluateExpression(retStmt.Value) : null;
                throw new ReturnException(retVal);

            case ImportStatementNode importStmt:
                ExecuteImportStatement(importStmt);
                return null;

            case TryStatementNode tryStmt:
                ExecuteTryStatement(tryStmt);
                return null;

            case ClassDeclarationNode classDecl:
                ExecuteClassDeclaration(classDecl);
                return null;

            case ThrowStatementNode throwStmt:
                var throwVal = EvaluateExpression(throwStmt.Expression);
                throw new YviraRuntimeException(throwVal);

            case TimeScheduleStatementNode timeStmt:
                ExecuteTimeScheduleStatement(timeStmt);
                return null;

            case StopScheduleStatementNode stopStmt:
                ExecuteStopScheduleStatement(stopStmt);
                return null;

            case DbConnectStatementNode dbConnStmt:
                ExecuteDbConnectStatement(dbConnStmt);
                return null;

            case DbExecStatementNode dbExecStmt:
                return EvaluateDbExec(dbExecStmt.ExecNode);

            case DbQueryStatementNode dbQueryStmt:
                return EvaluateDbQuery(dbQueryStmt.QueryNode);

            case DbTransactionStatementNode dbTxStmt:
                return ExecuteDbTransaction(dbTxStmt);

            case MatchStatementNode matchStmt:
                return ExecuteMatchStatement(matchStmt);

            case DieStatementNode dieStmt:
                ExecuteDieStatement(dieStmt);
                return null;

            case SleepStatementNode sleepStmt:
                ExecuteSleepStatement(sleepStmt);
                return null;

            case PdfDirectiveStatementNode pdfStmt:
                ExecutePdfDirective(pdfStmt);
                return null;

            default:
                throw new Exception($"Comando não suportado pelo interpretador: {statement.GetType().Name}");
        }
    }

    private void ExecuteTimeScheduleStatement(TimeScheduleStatementNode stmt)
    {
        var intervalVal = EvaluateExpression(stmt.Interval);
        double intervalSeconds = 1.0;
        if (intervalVal is double d) intervalSeconds = d;
        else if (intervalVal is int i) intervalSeconds = i;
        else if (intervalVal is long l) intervalSeconds = l;
        else if (intervalVal is float f) intervalSeconds = f;
        else if (double.TryParse(intervalVal?.ToString(), out double parsed)) intervalSeconds = parsed;

        if (intervalSeconds <= 0)
        {
            throw new Exception($"Intervalo para @time.{stmt.FunctionName} deve ser maior que 0 segundos.");
        }

        var callable = _environment.Get(stmt.FunctionName);
        if (callable == null)
        {
            throw new Exception($"Funcao '{stmt.FunctionName}' nao encontrada para agendamento temporal @time.");
        }

        var args = new object?[stmt.Arguments.Count];
        for (int idx = 0; idx < stmt.Arguments.Count; idx++)
        {
            args[idx] = EvaluateExpression(stmt.Arguments[idx]);
        }

        _scheduler.Schedule(stmt.FunctionName, intervalSeconds, callable, args, task =>
        {
            try
            {
                lock (this)
                {
                    if (task.Callable is YviraFunction fn)
                    {
                        var argList = new List<object?>(task.Arguments);
                        fn.Call(this, argList);
                    }
                    else if (task.Callable is Delegate del)
                    {
                        del.DynamicInvoke(task.Arguments);
                    }
                }
            }
            catch (Exception ex)
            {
                _outputHandler($"[ERRO @time.{task.Name}]: {ex.Message}");
            }
        });
    }

    private void ExecuteStopScheduleStatement(StopScheduleStatementNode stmt)
    {
        _scheduler.Stop(stmt.FunctionName);
    }

    private void ExecuteDbConnectStatement(DbConnectStatementNode stmt)
    {
        var uriObj = EvaluateExpression(stmt.UriExpression);
        string uri = uriObj?.ToString() ?? "";
        _dbManager.Connect(stmt.Alias ?? "default", uri, _currentDirectory);
    }

    private object? EvaluateDbExec(DbExecNode node)
    {
        var sqlObj = EvaluateExpression(node.SqlExpression);
        string sql = sqlObj?.ToString() ?? "";
        var parameters = ExtractDbParameters(node.ParametersExpression);

        bool requestFull = false;
        if (node.OptionsExpression != null)
        {
            var optionsObj = EvaluateExpression(node.OptionsExpression);
            if (optionsObj is YviraMap optMap)
            {
                var fullVal = optMap.Get("full");
                if (fullVal is bool b) requestFull = b;
                else if (fullVal is string s && bool.TryParse(s, out bool pb)) requestFull = pb;
            }
        }

        if (requestFull)
        {
            var detail = _dbManager.ExecuteNonQueryDetailed(node.Alias, sql, parameters);
            var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["rows_affected"] = (double)detail.RowsAffected,
                ["last_id"] = (double)detail.LastInsertId,
                ["success"] = detail.Success
            };
            return new YviraMap(map);
        }

        return (double)_dbManager.ExecuteNonQuery(node.Alias, sql, parameters);
    }

    private object? ExecuteDbTransaction(DbTransactionStatementNode stmt)
    {
        _dbManager.BeginTransaction(stmt.Alias);
        try
        {
            ExecuteBlock(stmt.Body);
            _dbManager.Commit(stmt.Alias);
            return null;
        }
        catch
        {
            try
            {
                _dbManager.Rollback(stmt.Alias);
            }
            catch { }
            throw;
        }
    }

    private object? EvaluateDbQuery(DbQueryNode node)
    {
        var sqlObj = EvaluateExpression(node.SqlExpression);
        string sql = sqlObj?.ToString() ?? "";
        var parameters = ExtractDbParameters(node.ParametersExpression);
        var rows = _dbManager.ExecuteQuery(node.Alias, sql, parameters);
        return new YviraList(rows);
    }

    private IList<object?>? ExtractDbParameters(ExpressionNode? paramsNode)
    {
        if (paramsNode == null) return null;
        var evaluated = EvaluateExpression(paramsNode);
        if (evaluated is YviraList list)
        {
            return (IList<object?>)list.Items;
        }
        if (evaluated is YviraMatrix matrix)
        {
            var matList = new List<object?>();
            for (int r = 0; r < matrix.Rows; r++)
            {
                for (int c = 0; c < matrix.Cols; c++)
                {
                    matList.Add(matrix.Get(r, c));
                }
            }
            return matList;
        }
        if (evaluated is IList<object?> objList)
        {
            return objList;
        }
        if (evaluated is object[] arr)
        {
            return new List<object?>(arr);
        }
        return new List<object?> { evaluated };
    }

    private void ExecuteTryStatement(TryStatementNode tryStmt)
    {
        try
        {
            ExecuteBlock(tryStmt.TryBlock);
        }
        catch (Exception ex) when (!(ex is ReturnException || ex is BreakException || ex is ContinueException))
        {
            if (tryStmt.CatchBlock != null)
            {
                var catchEnv = new Environment(_environment);
                if (tryStmt.CatchVariable != null)
                {
                    object? errorValue = ex is YviraRuntimeException yex ? yex.Value : ex.Message;
                    catchEnv.Define(tryStmt.CatchVariable.Name, errorValue);
                }
                ExecuteBlockWithEnv(tryStmt.CatchBlock, catchEnv);
            }
            else
            {
                throw; // Se s� tem finally, re-lan�a
            }
        }
        finally
        {
            if (tryStmt.FinallyBlock != null)
            {
                ExecuteBlock(tryStmt.FinallyBlock);
            }
        }
    }

    private void ExecuteImportStatement(ImportStatementNode importStmt)
    {
        string rawPath = importStmt.ModulePath;
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            throw new Exception("Erro de Importa��o: Caminho do m�dulo n�o pode ser vazio.");
        }

        // M�dulos nativos embutidos da biblioteca padr�o Yvira
        if (rawPath.Equals("fs", StringComparison.OrdinalIgnoreCase) || rawPath.Equals("filesystem", StringComparison.OrdinalIgnoreCase))
        {
            var fsModule = FileSystemModule.CreateModule(_currentDirectory);
            ApplyModuleSymbols(fsModule, importStmt.Alias);
            return;
        }

        if (rawPath.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            var httpModule = HttpModule.CreateModule(this);
            if (string.IsNullOrEmpty(importStmt.Alias))
            {
                _environment.Define("http", httpModule);
                ApplyModuleSymbols(httpModule, null);
            }
            else
            {
                ApplyModuleSymbols(httpModule, importStmt.Alias);
            }
            return;
        }

        if (rawPath.Equals("cache", StringComparison.OrdinalIgnoreCase))
        {
            var cacheModule = CacheModule.CreateModule(this);
            if (string.IsNullOrEmpty(importStmt.Alias))
            {
                _environment.Define("cache", cacheModule);
                ApplyModuleSymbols(cacheModule, null);
            }
            else
            {
                ApplyModuleSymbols(cacheModule, importStmt.Alias);
            }
            return;
        }

        if (rawPath.Equals("doc", StringComparison.OrdinalIgnoreCase) || rawPath.Equals("document", StringComparison.OrdinalIgnoreCase) || rawPath.Equals("pdf", StringComparison.OrdinalIgnoreCase) || rawPath.Equals("excel", StringComparison.OrdinalIgnoreCase))
        {
            var docModule = DocumentModule.CreateModule(_currentDirectory);
            if (string.IsNullOrEmpty(importStmt.Alias))
            {
                _environment.Define("doc", docModule);
                ApplyModuleSymbols(docModule, null);
            }
            else
            {
                ApplyModuleSymbols(docModule, importStmt.Alias);
            }
            return;
        }

        // Resolve caminho do arquivo ou pacote
        string fullPath;
        if (rawPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            fullPath = PackageManager.FetchUrlModule(rawPath);
        }
        else if (System.IO.Path.IsPathRooted(rawPath))
        {
            fullPath = System.IO.Path.GetFullPath(rawPath);
        }
        else
        {
            fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(_currentDirectory, rawPath));
        }

        // Se n�o encontrar o arquivo como est� e n�o tiver extens�o, tenta com .yv
        if (!System.IO.File.Exists(fullPath) && !rawPath.EndsWith(".yv", StringComparison.OrdinalIgnoreCase) && !rawPath.EndsWith(".env", StringComparison.OrdinalIgnoreCase) && !rawPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            string withYv = fullPath + ".yv";
            if (System.IO.File.Exists(withYv))
            {
                fullPath = withYv;
            }
        }

        // Se ainda n�o encontrou, busca no Gerenciador de Pacotes (.yvira/packages ou cache global)
        if (!System.IO.File.Exists(fullPath))
        {
            string? pkgPath = PackageManager.ResolvePackagePath(rawPath, _currentDirectory);
            if (pkgPath != null && System.IO.File.Exists(pkgPath))
            {
                fullPath = pkgPath;
            }
        }

        if (!System.IO.File.Exists(fullPath))
        {
            throw new Exception($"Erro de Importa��o: Arquivo ou pacote n�o encontrado: '{rawPath}' (Caminho verificado: '{fullPath}')");
        }

        string ext = System.IO.Path.GetExtension(fullPath).ToLowerInvariant();
        string fileName = System.IO.Path.GetFileName(fullPath).ToLowerInvariant();

        // M�dulos nativos embutidos (Built-in Standard Library)
        if (rawPath.Equals("fs", StringComparison.OrdinalIgnoreCase) || rawPath.Equals("filesystem", StringComparison.OrdinalIgnoreCase))
        {
            var fsModule = FileSystemModule.CreateModule(_currentDirectory);
            ApplyModuleSymbols(fsModule, importStmt.Alias);
            return;
        }

        if (rawPath.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            var httpModule = HttpModule.CreateModule(this);
            if (string.IsNullOrEmpty(importStmt.Alias))
            {
                _environment.Define("http", httpModule);
                ApplyModuleSymbols(httpModule, null);
            }
            else
            {
                ApplyModuleSymbols(httpModule, importStmt.Alias);
            }
            return;
        }

        if (rawPath.Equals("cache", StringComparison.OrdinalIgnoreCase))
        {
            var cacheModule = CacheModule.CreateModule(this);
            if (string.IsNullOrEmpty(importStmt.Alias))
            {
                _environment.Define("cache", cacheModule);
                ApplyModuleSymbols(cacheModule, null);
            }
            else
            {
                ApplyModuleSymbols(cacheModule, importStmt.Alias);
            }
            return;
        }

        // 1. Suporte a arquivo .env (ou terminando em .env)
        if (ext == ".env" || fileName == ".env" || fileName.EndsWith(".env"))
        {
            string envContent = System.IO.File.ReadAllText(fullPath);
            var envMap = FileParsers.ParseEnv(envContent);
            ApplyModuleMap(envMap, importStmt.Alias);
            return;
        }

        // 2. Suporte a arquivo .json
        if (ext == ".json")
        {
            string jsonContent = System.IO.File.ReadAllText(fullPath);
            var jsonObj = FileParsers.ParseJson(jsonContent);
            if (jsonObj is YviraMap jsonMap)
            {
                ApplyModuleMap(jsonMap, importStmt.Alias);
            }
            else
            {
                string aliasName = importStmt.Alias ?? System.IO.Path.GetFileNameWithoutExtension(fullPath);
                _environment.Define(aliasName, jsonObj);
            }
            return;
        }

        // 3. Suporte a arquivos .yv (M�dulos Yvira)
        if (_moduleCache.TryGetValue(fullPath, out var cachedMap))
        {
            ApplyModuleSymbols(cachedMap, importStmt.Alias);
            return;
        }

        if (_executingModules.Contains(fullPath))
        {
            return;
        }
        _executingModules.Add(fullPath);

        if (_moduleLoader == null)
        {
            throw new Exception("Erro de Importa��o: M�dulo carregador n�o foi configurado no interpretador.");
        }

        string sourceCode = System.IO.File.ReadAllText(fullPath);
        var program = _moduleLoader(sourceCode);

        string previousDir = _currentDirectory;
        _currentDirectory = System.IO.Path.GetDirectoryName(fullPath) ?? _currentDirectory;
        var moduleEnv = new Environment(_environment);
        var previousEnv = _environment;
        _environment = moduleEnv;

        try
        {
            foreach (var stmt in program.Statements)
            {
                ExecuteStatement(stmt);
            }
        }
        finally
        {
            _environment = previousEnv;
            _currentDirectory = previousDir;
            _executingModules.Remove(fullPath);
        }

        var moduleDict = new Dictionary<string, object?>();
        foreach (var kvp in moduleEnv.GetAllLocal())
        {
            moduleDict[kvp.Key] = kvp.Value;
        }
        var exports = new YviraMap(moduleDict);
        _moduleCache[fullPath] = exports;
        ApplyModuleSymbols(exports, importStmt.Alias);
    }

    private void ApplyModuleMap(YviraMap map, string? alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            foreach (var kvp in map)
            {
                if (!_environment.IsDefinedLocally(kvp.Key))
                {
                    _environment.Define(kvp.Key, kvp.Value);
                }
            }
        }
        else
        {
            _environment.Define(alias, map);
        }
    }

    private void ApplyModuleSymbols(YviraMap exports, string? alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            foreach (var kvp in exports)
            {
                if (!_environment.IsDefinedLocally(kvp.Key))
                {
                    _environment.Define(kvp.Key, kvp.Value);
                }
            }
        }
        else
        {
            _environment.Define(alias, exports);
        }
    }
    private void ExecuteVariableDeclaration(VariableDeclarationNode varDecl)
    {
        object? initialValue = null;
        if (varDecl.Initializer != null)
        {
            initialValue = EvaluateExpression(varDecl.Initializer);
        }

        _environment.Define(varDecl.Identifier.Name, initialValue, varDecl.IsConst);
    }

    private void ExecuteClassDeclaration(ClassDeclarationNode classDecl)
    {
        YviraClass? superClass = null;
        if (classDecl.SuperClass != null)
        {
            var superObj = _environment.Get(classDecl.SuperClass.Name);
            if (superObj is YviraClass sc)
            {
                superClass = sc;
            }
            else
            {
                throw new Exception($"Erro de Heranca: Superclasse '{classDecl.SuperClass.Name}' nao e uma classe valida.");
            }
        }

        var methods = new Dictionary<string, YviraFunction>(StringComparer.OrdinalIgnoreCase);
        var staticMethods = new Dictionary<string, YviraFunction>(StringComparer.OrdinalIgnoreCase);

        foreach (var methodDecl in classDecl.Methods)
        {
            var yvFunc = new YviraFunction(methodDecl.Function, _environment);
            if (methodDecl.IsStatic)
            {
                staticMethods[methodDecl.Function.Name.Name] = yvFunc;
            }
            else
            {
                methods[methodDecl.Function.Name.Name] = yvFunc;
            }
        }

        var yvClass = new YviraClass(classDecl.Name.Name, superClass, methods, staticMethods);
        _environment.Define(classDecl.Name.Name, yvClass);
    }

    private void ExecuteFunctionDeclaration(FunctionDeclarationNode funcDecl)
    {
        var function = new YviraFunction(funcDecl, _environment);
        _environment.Define(funcDecl.Name.Name, function);
    }

    public void ExecuteBlockWithEnv(BlockStatementNode block, Environment env)
    {
        var previousEnv = _environment;
        try
        {
            _environment = env;
            foreach (var stmt in block.Statements)
            {
                ExecuteStatement(stmt);
            }
        }
        finally
        {
            _environment = previousEnv;
        }
    }

    private void ExecuteBlock(BlockStatementNode block)
    {
        var previousEnv = _environment;
        try
        {
            _environment = new Environment(previousEnv);
            foreach (var stmt in block.Statements)
            {
                ExecuteStatement(stmt);
            }
        }
        finally
        {
            _environment = previousEnv;
        }
    }

    private void ExecuteIfStatement(IfStatementNode ifStmt)
    {
        var conditionValue = EvaluateExpression(ifStmt.Condition);

        if (IsTruthy(conditionValue))
        {
            ExecuteStatement(ifStmt.ThenBranch);
        }
        else if (ifStmt.ElseBranch != null)
        {
            ExecuteStatement(ifStmt.ElseBranch);
        }
    }

    private void ExecuteWhileStatement(WhileStatementNode whileStmt)
    {
        while (IsTruthy(EvaluateExpression(whileStmt.Condition)))
        {
            try
            {
                ExecuteStatement(whileStmt.Body);
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                continue;
            }
        }
    }

    private void ExecuteForStatement(ForStatementNode forStmt)
    {
        var previousEnv = _environment;
        _environment = new Environment(previousEnv);
        try
        {
            if (forStmt.Initializer != null)
            {
                ExecuteStatement(forStmt.Initializer);
            }

            while (forStmt.Condition == null || IsTruthy(EvaluateExpression(forStmt.Condition)))
            {
                try
                {
                    ExecuteStatement(forStmt.Body);
                }
                catch (BreakException)
                {
                    break;
                }
                catch (ContinueException)
                {
                    // avanca para incremento
                }

                if (forStmt.Increment != null)
                {
                    EvaluateExpression(forStmt.Increment);
                }
            }
        }
        finally
        {
            _environment = previousEnv;
        }
    }

    private void ExecuteForeachStatement(ForeachStatementNode foreachStmt)
    {
        var collectionVal = EvaluateExpression(foreachStmt.Collection);
        System.Collections.IEnumerable enumerable;

        if (collectionVal is System.Collections.IEnumerable e && !(collectionVal is string))
        {
            enumerable = e;
        }
        else if (collectionVal is string s)
        {
            var charList = new List<string>();
            foreach (char ch in s) charList.Add(ch.ToString());
            enumerable = charList;
        }
        else
        {
            throw new Exception($"Erro de Execu��o: Objeto '{collectionVal?.GetType().Name}' n�o � iter�vel.");
        }

        var previousEnv = _environment;
        _environment = new Environment(previousEnv);
        try
        {
            _environment.Define(foreachStmt.Variable.Name, null);
            foreach (var item in enumerable)
            {
                object? currentItem = item;
                if (item is KeyValuePair<string, object?> kvp)
                {
                    currentItem = kvp.Key;
                }

                _environment.Assign(foreachStmt.Variable.Name, currentItem);
                try
                {
                    ExecuteStatement(foreachStmt.Body);
                }
                catch (BreakException)
                {
                    break;
                }
                catch (ContinueException)
                {
                    continue;
                }
            }
        }
        finally
        {
            _environment = previousEnv;
        }
    }

    public object? EvaluateExpression(ExpressionNode expression)
    {
        return expression switch
        {
            NumberLiteralNode num => num.Value,
            StringLiteralNode str => str.Value,
            BooleanLiteralNode b => b.Value,
            NullLiteralNode => null,
            IdentifierNode id => _environment.Get(id.Name),
            ParenthesizedExpressionNode paren => EvaluateExpression(paren.Expression),
            UnaryExpressionNode un => EvaluateUnary(un),
            BinaryExpressionNode bin => EvaluateBinary(bin),
            TernaryExpressionNode tern => EvaluateTernary(tern),
            AssignmentExpressionNode assign => EvaluateAssignment(assign),
            CallExpressionNode call => EvaluateCall(call),
            ArrayLiteralNode arr => EvaluateArray(arr),
            MapLiteralNode map => EvaluateMap(map),
            IndexAccessNode idx => EvaluateIndexAccess(idx),
            ThisExpressionNode => _environment.Get("this"),
            DbExecNode dbExec => EvaluateDbExec(dbExec),
            DbQueryNode dbQuery => EvaluateDbQuery(dbQuery),
            RealtimeExpressionNode rtExpr => EvaluateRealtime(rtExpr),
            InterpolatedStringNode interp => EvaluateInterpolatedString(interp),
            MemberAssignmentExpressionNode memberAssign => EvaluateMemberAssignment(memberAssign),
            IndexAssignmentExpressionNode indexAssign => EvaluateIndexAssignment(indexAssign),
            MemberAccessNode member => EvaluateMemberAccess(member),
            MethodCallNode method => EvaluateMethodCall(method),
            _ => throw new Exception($"Expressão desconhecida para avaliação: {expression.GetType().Name}")
        };
    }

    private object? EvaluateTernary(TernaryExpressionNode tern)
    {
        var conditionVal = EvaluateExpression(tern.Condition);
        if (IsTruthy(conditionVal))
        {
            return EvaluateExpression(tern.TrueBranch);
        }
        else
        {
            return EvaluateExpression(tern.FalseBranch);
        }
    }

    private object EvaluateArray(ArrayLiteralNode arr)
    {
        var elements = new List<object?>();
        bool hasNonNumeric = false;
        foreach (var el in arr.Elements)
        {
            var val = EvaluateExpression(el);
            elements.Add(val);
            if (val != null && !(val is double or float or int or long or short or byte or YviraMatrix))
            {
                hasNonNumeric = true;
            }
        }

        // Se for lista vazia [] ou contiver tipos n�o-num�ricos, retorna YviraList
        if (elements.Count == 0 || hasNonNumeric)
        {
            return new YviraList(elements);
        }

        return YviraMatrix.FromElements(elements);
    }

    private object EvaluateMap(MapLiteralNode map)
    {
        var ymap = new YviraMap();
        foreach (var entry in map.Entries)
        {
            var keyVal = EvaluateExpression(entry.Key)?.ToString() ?? "";
            var valVal = EvaluateExpression(entry.Value);
            ymap.Set(keyVal, valVal);
        }
        return ymap;
    }

    private object? EvaluateIndexAccess(IndexAccessNode idx)
    {
        var target = EvaluateExpression(idx.Target);

        if (target is YviraMatrix mat)
        {
            if (idx.Indices.Count == 1)
            {
                int index = Convert.ToInt32(EvaluateExpression(idx.Indices[0]));
                if (mat.Rows == 1)
                {
                    return mat.Get(0, index);
                }
                if (mat.Cols == 1)
                {
                    return mat.Get(index, 0);
                }
                // Se for matriz 2D e pediu m[i], devolve a linha inteira como vetor 1xCols!
                return mat.GetRow(index);
            }
            if (idx.Indices.Count == 2)
            {
                int row = Convert.ToInt32(EvaluateExpression(idx.Indices[0]));
                int col = Convert.ToInt32(EvaluateExpression(idx.Indices[1]));
                return mat.Get(row, col);
            }
            throw new Exception("Erro de Acesso: Índices de matriz devem ser [index] ou [linha, coluna].");
        }

        if (target is YviraList list)
        {
            if (idx.Indices.Count == 1)
            {
                int index = Convert.ToInt32(EvaluateExpression(idx.Indices[0]));
                return list.Get(index);
            }
            throw new Exception("Erro de Acesso: Listas aceitam apenas um �ndice [index].");
        }



        if (target is YviraMap map)
        {
            if (idx.Indices.Count == 1)
            {
                string key = EvaluateExpression(idx.Indices[0])?.ToString() ?? "";
                return map.Get(key);
            }
            throw new Exception("Erro de Acesso: Mapas aceitam apenas uma chave [chave].");
        }

        if (target is string str)
        {
            if (idx.Indices.Count == 1)
            {
                int index = Convert.ToInt32(EvaluateExpression(idx.Indices[0]));
                int actualIndex = index < 0 ? str.Length + index : index;
                if (actualIndex < 0 || actualIndex >= str.Length)
                {
                    throw new IndexOutOfRangeException($"�ndice fora dos limites da string: {index}");
                }
                return str[actualIndex].ToString();
            }
            throw new Exception("Erro de Acesso: Strings aceitam apenas um �ndice [index].");
        }

        throw new Exception($"Erro de Acesso: Objeto do tipo '{target?.GetType().Name}' n�o suporta indexa��o.");
    }

    private object? EvaluateIndexAssignment(IndexAssignmentExpressionNode idxAssign)
    {
        var target = EvaluateExpression(idxAssign.Target);
        var rightValue = EvaluateExpression(idxAssign.Value);

        if (target is YviraMatrix mat)
        {
            if (idxAssign.Indices.Count == 1)
            {
                int index = Convert.ToInt32(EvaluateExpression(idxAssign.Indices[0]));
                if (mat.Rows == 1)
                {
                    double curVal = mat.Get(0, index);
                    double computed = ComputeCompoundAssign(curVal, rightValue, idxAssign.OperatorToken);
                    mat.Set(0, index, computed);
                    return computed;
                }
            }
            if (idxAssign.Indices.Count == 2)
            {
                int r = Convert.ToInt32(EvaluateExpression(idxAssign.Indices[0]));
                int c = Convert.ToInt32(EvaluateExpression(idxAssign.Indices[1]));
                double curVal = mat.Get(r, c);
                double computed = ComputeCompoundAssign(curVal, rightValue, idxAssign.OperatorToken);
                mat.Set(r, c, computed);
                return computed;
            }
            throw new Exception("Erro de Atribuição: Índices de matriz devem ser [index] ou [linha, coluna].");
        }

        if (target is YviraList list)
        {
            if (idxAssign.Indices.Count == 1)
            {
                int index = Convert.ToInt32(EvaluateExpression(idxAssign.Indices[0]));
                if (idxAssign.OperatorToken.Type == TokenType.Assign)
                {
                    list.Set(index, rightValue);
                    return rightValue;
                }

                object? curVal = list.Get(index);
                object? computed = idxAssign.OperatorToken.Type switch
                {
                    TokenType.PlusAssign => (curVal is string || rightValue is string) ? $"{curVal}{rightValue}" : Convert.ToDouble(curVal) + Convert.ToDouble(rightValue),
                    TokenType.MinusAssign => Convert.ToDouble(curVal) - Convert.ToDouble(rightValue),
                    TokenType.StarAssign => Convert.ToDouble(curVal) * Convert.ToDouble(rightValue),
                    TokenType.SlashAssign => Convert.ToDouble(curVal) / Convert.ToDouble(rightValue),
                    _ => throw new Exception($"Operador não suportado: {idxAssign.OperatorToken.Text}")
                };
                list.Set(index, computed);
                return computed;
            }
            throw new Exception("Erro de Atribuição: Listas aceitam apenas um índice [index].");
        }

        if (target is YviraMap map)
        {
            if (idxAssign.Indices.Count == 1)
            {
                string key = EvaluateExpression(idxAssign.Indices[0])?.ToString() ?? "";
                if (idxAssign.OperatorToken.Type == TokenType.Assign)
                {
                    map.Set(key, rightValue);
                    return rightValue;
                }

                object? curVal = map.Get(key);
                object? computed = idxAssign.OperatorToken.Type switch
                {
                    TokenType.PlusAssign => (curVal is string || rightValue is string) ? $"{curVal}{rightValue}" : Convert.ToDouble(curVal) + Convert.ToDouble(rightValue),
                    TokenType.MinusAssign => Convert.ToDouble(curVal) - Convert.ToDouble(rightValue),
                    TokenType.StarAssign => Convert.ToDouble(curVal) * Convert.ToDouble(rightValue),
                    TokenType.SlashAssign => Convert.ToDouble(curVal) / Convert.ToDouble(rightValue),
                    _ => throw new Exception($"Operador não suportado: {idxAssign.OperatorToken.Text}")
                };
                map.Set(key, computed);
                return computed;
            }
            throw new Exception("Erro de Atribuição: Mapas aceitam apenas uma chave [chave].");
        }

        throw new Exception($"Erro de Atribuição: Objeto '{target?.GetType().Name}' não suporta atribuição por índice.");
    }

    private static double ComputeCompoundAssign(double curVal, object? rightValue, Token op)
    {
        double r = Convert.ToDouble(rightValue);
        return op.Type switch
        {
            TokenType.Assign => r,
            TokenType.PlusAssign => curVal + r,
            TokenType.MinusAssign => curVal - r,
            TokenType.StarAssign => curVal * r,
            TokenType.SlashAssign => curVal / r,
            _ => throw new Exception($"Operador não suportado: {op.Text}")
        };
    }

    private object? EvaluateMemberAssignment(MemberAssignmentExpressionNode memberAssign)
    {
        var target = EvaluateExpression(memberAssign.Target);
        var rightValue = EvaluateExpression(memberAssign.Value);

        if (target is YviraInstance instance)
        {
            if (memberAssign.OperatorToken.Type == TokenType.Assign)
            {
                instance.Set(memberAssign.MemberName, rightValue);
                return rightValue;
            }

            // Atribui��o composta (+=, -=, *=, /=)
            object? currentVal = null;
            try { currentVal = instance.Get(memberAssign.MemberName); } catch { }

            object? computed = memberAssign.OperatorToken.Type switch
            {
                TokenType.PlusAssign => (currentVal is string || rightValue is string)
                    ? (currentVal?.ToString() + rightValue?.ToString())
                    : (Convert.ToDouble(currentVal) + Convert.ToDouble(rightValue)),
                TokenType.MinusAssign => Convert.ToDouble(currentVal) - Convert.ToDouble(rightValue),
                TokenType.StarAssign => Convert.ToDouble(currentVal) * Convert.ToDouble(rightValue),
                TokenType.SlashAssign => Convert.ToDouble(currentVal) / Convert.ToDouble(rightValue),
                _ => throw new Exception($"Operador de atribui��o n�o suportado: {memberAssign.OperatorToken.Text}")
            };

            instance.Set(memberAssign.MemberName, computed);
            return computed;
        }



        if (target is YviraMap map)
        {
            if (memberAssign.OperatorToken.Type == TokenType.Assign)
            {
                map.Set(memberAssign.MemberName, rightValue);
                return rightValue;
            }

            object? currentVal = map.Get(memberAssign.MemberName);
            object? computed = memberAssign.OperatorToken.Type switch
            {
                TokenType.PlusAssign => (currentVal is string || rightValue is string)
                    ? (currentVal?.ToString() + rightValue?.ToString())
                    : (Convert.ToDouble(currentVal) + Convert.ToDouble(rightValue)),
                TokenType.MinusAssign => Convert.ToDouble(currentVal) - Convert.ToDouble(rightValue),
                TokenType.StarAssign => Convert.ToDouble(currentVal) * Convert.ToDouble(rightValue),
                TokenType.SlashAssign => Convert.ToDouble(currentVal) / Convert.ToDouble(rightValue),
                _ => throw new Exception($"Operador de atribui��o n�o suportado: {memberAssign.OperatorToken.Text}")
            };

            map.Set(memberAssign.MemberName, computed);
            return computed;
        }

        throw new Exception($"Erro de Atribui��o: N�o � poss�vel definir propriedades no tipo '{target?.GetType().Name}'.");
    }

    private object? EvaluateMemberAccess(MemberAccessNode member)
    {
        var target = EvaluateExpression(member.Target);

        if (target is YviraMatrix mat)
        {
            return member.MemberName switch
            {
                "rows" or "linhas" => (double)mat.Rows,
                "cols" or "colunas" => (double)mat.Cols,
                "length" or "len" => (double)(mat.Rows > 1 ? mat.Rows : mat.Length),
                "T" => mat.Transpose(),
                "det" => mat.Determinant(),
                "trace" => mat.Trace(),
                "sum" => mat.Sum(),
                "mean" => mat.Mean(),
                "max" => mat.Max(),
                "min" => mat.Min(),
                _ => throw new Exception($"Erro de Membro: Propriedade '{member.MemberName}' não existe na Matriz.")
            };
        }

        if (target is string s)
        {
            return member.MemberName switch
            {
                "length" or "len" => (double)s.Length,
                "upper" => s.ToUpperInvariant(),
                "lower" => s.ToLowerInvariant(),
                "trim" => s.Trim(),
                _ => throw new Exception($"Erro de Membro: Propriedade '{member.MemberName}' n�o existe em String.")
            };
        }

        if (target is YviraList l)
        {
            return member.MemberName switch
            {
                "length" or "len" or "count" => (double)l.Count,
                _ => throw new Exception($"Erro de Membro: Propriedade '{member.MemberName}' n�o existe em Lista.")
            };
        }

        if (target is YviraClass yvCls)
        {
            var sm = yvCls.FindStaticMethod(member.MemberName);
            if (sm != null) return sm;
            throw new Exception($"Erro de Membro Estatico: Metodo ou propriedade estatica '{member.MemberName}' nao existe na classe '{yvCls.Name}'.");
        }

        if (target is YviraInstance instance)
        {
            return instance.Get(member.MemberName);
        }

        if (target is YviraMap m)
        {
            return member.MemberName switch
            {
                "length" or "len" or "count" => (double)m.Count,
                "keys" => new YviraList(m.Keys),
                "values" => new YviraList(m.Values),
                _ => m.Get(member.MemberName)
            };
        }

        throw new Exception($"Erro de Membro: Tipo '{target?.GetType().Name}' n�o possui propriedades acess�veis.");
    }

    private object? EvaluateMethodCall(MethodCallNode method)
    {
        var target = EvaluateExpression(method.Target);
        var args = new List<object?>();
        foreach (var a in method.Arguments)
        {
            args.Add(EvaluateExpression(a));
        }
        


        if (target is YviraClass yvCls)
        {
            var sm = yvCls.FindStaticMethod(method.MethodName);
            if (sm != null)
            {
                return sm.Call(this, args);
            }
            throw new Exception($"Erro de Metodo Estatico: Metodo estatico '{method.MethodName}' nao encontrado na classe '{yvCls.Name}'.");
        }

        if (target is YviraInstance instance)
        {
            var methodOrProp = instance.Get(method.MethodName);
            if (methodOrProp is YviraFunction yvFunc)
            {
                return yvFunc.Call(this, args);
            }
            throw new Exception($"Erro de M�todo: '{method.MethodName}' n�o � um m�todo execut�vel na classe '{instance.Class.Name}'.");
        }

        if (target is YviraMatrix mat)
        {
            return method.MethodName switch
            {
                "get" when args.Count == 1 => (mat.Rows == 1 ? mat.Get(0, Convert.ToInt32(args[0])) : mat.GetRow(Convert.ToInt32(args[0]))),
                "get" when args.Count == 2 => mat.Get(Convert.ToInt32(args[0]), Convert.ToInt32(args[1])),
                "row" when args.Count == 1 => mat.GetRow(Convert.ToInt32(args[0])),
                "col" when args.Count == 1 => mat.GetCol(Convert.ToInt32(args[0])),
                "transpose" => mat.Transpose(),
                "inverse" or "inv" => mat.Inverse(),
                "det" or "determinant" => mat.Determinant(),
                "trace" => mat.Trace(),
                "sum" => mat.Sum(),
                "mean" => mat.Mean(),
                "max" => mat.Max(),
                "min" => mat.Min(),
                "solve" when args.Count == 1 && args[0] is YviraMatrix bMat => mat.Solve(bMat),
                _ => throw new Exception($"Erro de Método: Método '{method.MethodName}' inválido ou argumentos incompatíveis para Matriz.")
            };
        }

        if (target is string str)
        {
            return method.MethodName switch
            {
                "upper" => str.ToUpperInvariant(),
                "lower" => str.ToLowerInvariant(),
                "trim" => str.Trim(),
                "contains" when args.Count >= 1 => str.Contains(args[0]?.ToString() ?? ""),
                "startsWith" or "starts_with" when args.Count >= 1 => str.StartsWith(args[0]?.ToString() ?? ""),
                "endsWith" or "ends_with" when args.Count >= 1 => str.EndsWith(args[0]?.ToString() ?? ""),
                "indexOf" or "index_of" when args.Count >= 1 => (double)str.IndexOf(args[0]?.ToString() ?? ""),
                "replace" when args.Count >= 2 => str.Replace(args[0]?.ToString() ?? "", args[1]?.ToString() ?? ""),
                "split" => new YviraList(str.Split(args.Count >= 1 ? args[0]!.ToString()! : " ").Select(x => (object?)x)),
                "substring" or "sub" => SubstringHelper(str, args),
                _ => throw new Exception($"Erro de M�todo: M�todo '{method.MethodName}' n�o suportado para String.")
            };
        }

        if (target is YviraList list)
        {
            switch (method.MethodName)
            {
                case "add":
                case "append":
                case "push":
                    if (args.Count >= 1) list.Add(args[0]);
                    return list;

                case "pop":
                    int popIdx = args.Count >= 1 ? Convert.ToInt32(args[0]) : -1;
                    return list.Pop(popIdx);

                case "insert":
                    if (args.Count >= 2) list.Insert(Convert.ToInt32(args[0]), args[1]);
                    return list;

                case "remove":
                    if (args.Count >= 1)
                    {
                        int rmIdx = list.IndexOf(args[0]);
                        if (rmIdx >= 0) list.Pop(rmIdx);
                    }
                    return list;

                case "contains":
                    return args.Count >= 1 && list.Contains(args[0]);

                case "indexOf":
                case "index_of":
                    return args.Count >= 1 ? (double)list.IndexOf(args[0]) : -1.0;

                case "clear":
                    list.Clear();
                    return null;

                case "tamanho":
                case "size":
                case "length":
                case "count":
                    return (double)list.Count;

                case "join":
                    string sep = args.Count >= 1 ? args[0]?.ToString() ?? "" : ", ";
                    return string.Join(sep, list.Select(x => x?.ToString() ?? ""));

                default:
                    throw new Exception($"Erro de M�todo: M�todo '{method.MethodName}' n�o suportado para Lista.");
            }
        }



        if (target is YviraMap map)
        {
            switch (method.MethodName)
            {
                case "get":
                    if (map.HasKey("get"))
                    {
                        var customGet = map.Get("get");
                        if (customGet is HostFunction hostGet) return hostGet.Call(this, args);
                        if (customGet is YviraFunction yvGet) return yvGet.Call(this, args);
                    }
                    return args.Count >= 1 ? map.Get(args[0]?.ToString() ?? "") : null;

                case "set":
                    if (map.HasKey("set"))
                    {
                        var customSet = map.Get("set");
                        if (customSet is HostFunction hostSet) return hostSet.Call(this, args);
                        if (customSet is YviraFunction yvSet) return yvSet.Call(this, args);
                    }
                    if (args.Count >= 2) map.Set(args[0]?.ToString() ?? "", args[1]);
                    return map;

                case "has":
                case "containsKey":
                case "contains":
                    if (map.HasKey(method.MethodName))
                    {
                        var customFn = map.Get(method.MethodName);
                        if (customFn is HostFunction hf) return hf.Call(this, args);
                        if (customFn is YviraFunction yf) return yf.Call(this, args);
                    }
                    return args.Count >= 1 && map.HasKey(args[0]?.ToString() ?? "");

                case "remove":
                case "delete":
                    if (map.HasKey(method.MethodName))
                    {
                        var customFn = map.Get(method.MethodName);
                        if (customFn is HostFunction hf) return hf.Call(this, args);
                        if (customFn is YviraFunction yf) return yf.Call(this, args);
                    }
                    return args.Count >= 1 && map.Remove(args[0]?.ToString() ?? "");

                case "clear":
                    if (map.HasKey("clear"))
                    {
                        var customFn = map.Get("clear");
                        if (customFn is HostFunction hf) return hf.Call(this, args);
                        if (customFn is YviraFunction yf) return yf.Call(this, args);
                    }
                    map.Clear();
                    return null;

                case "keys":
                    return new YviraList(map.Keys.Select(k => (object?)k));

                case "values":
                    return new YviraList(map.Values);

                default:
                    if (map.HasKey(method.MethodName))
                    {
                        var memberVal = map.Get(method.MethodName);
                        if (memberVal is YviraFunction fn)
                        {
                            return fn.Call(this, args);
                        }
                        if (memberVal is HostFunction hostFn)
                        {
                            return hostFn.Call(this, args);
                        }
                    }
                    throw new Exception($"Erro de M�todo: M�todo '{method.MethodName}' n�o suportado para Map.");
            }
        }

        throw new Exception($"Erro de M�todo: Tipo '{target?.GetType().Name}' n�o possui m�todos.");
    }

    private static string SubstringHelper(string str, List<object?> args)
    {
        if (args.Count == 0) return str;
        int start = Convert.ToInt32(args[0]);
        if (start < 0) start = Math.Max(0, str.Length + start);
        if (start > str.Length) return "";
        if (args.Count == 1) return str.Substring(start);
        int length = Convert.ToInt32(args[1]);
        if (start + length > str.Length) length = str.Length - start;
        return str.Substring(start, Math.Max(0, length));
    }

    private object? EvaluateCall(CallExpressionNode call)
    {
        string funcName = call.Callee.Name;
        var evaluatedArgs = new List<object?>();
        foreach (var arg in call.Arguments)
        {
            evaluatedArgs.Add(EvaluateExpression(arg));
        }

        // Funções nativas da biblioteca padrão da linguagem Yvira
        switch (funcName)
        {
            case "list":
                return new YviraList(evaluatedArgs);

            case "len":
            case "length":
                if (evaluatedArgs.Count > 0)
                {
                    var a0 = evaluatedArgs[0];
                    if (a0 is string s) return (double)s.Length;
                    if (a0 is YviraList l) return (double)l.Count;
                    if (a0 is YviraMap m) return (double)m.Count;
                    if (a0 is YviraMatrix mx) return (double)(mx.Rows > 1 ? mx.Rows : mx.Length);
                }
                return 0.0;

            case "range":
                if (evaluatedArgs.Count == 1)
                {
                    int stop = Convert.ToInt32(evaluatedArgs[0]);
                    var rList = new YviraList();
                    for (int i = 0; i < stop; i++) rList.Add((double)i);
                    return rList;
                }
                if (evaluatedArgs.Count >= 2)
                {
                    int start = Convert.ToInt32(evaluatedArgs[0]);
                    int stop = Convert.ToInt32(evaluatedArgs[1]);
                    int step = evaluatedArgs.Count > 2 ? Convert.ToInt32(evaluatedArgs[2]) : 1;
                    var rList = new YviraList();
                    if (step > 0)
                    {
                        for (int i = start; i < stop; i += step) rList.Add((double)i);
                    }
                    else if (step < 0)
                    {
                        for (int i = start; i > stop; i += step) rList.Add((double)i);
                    }
                    return rList;
                }
                return new YviraList();

            case "print":
            case "println":
                string output = string.Join(" ", evaluatedArgs.ConvertAll(a => a?.ToString() ?? "null"));
                _outputHandler(output);
                return null;

            case "input":
                if (evaluatedArgs.Count > 0 && evaluatedArgs[0] != null)
                {
                    Console.Write(evaluatedArgs[0]!.ToString());
                }
                return _inputHandler() ?? "";

            // --- Built-ins de Interface TUI / Gráfica no Terminal ---
            case "menu":
            case "menu_interativo":
                if (evaluatedArgs.Count > 0)
                {
                    string mTitle = "";
                    var optsList = new List<string>();

                    if (evaluatedArgs.Count >= 2)
                    {
                        mTitle = evaluatedArgs[0]?.ToString() ?? "";
                        if (evaluatedArgs[1] is YviraList yl)
                        {
                            foreach (var item in yl.Items) optsList.Add(item?.ToString() ?? "");
                        }
                        else if (evaluatedArgs[1] is IEnumerable<object> en)
                        {
                            foreach (var item in en) optsList.Add(item?.ToString() ?? "");
                        }
                    }
                    else if (evaluatedArgs[0] is YviraList ylSingle)
                    {
                        foreach (var item in ylSingle.Items) optsList.Add(item?.ToString() ?? "");
                    }
                    else if (evaluatedArgs[0] is IEnumerable<object> enSingle)
                    {
                        foreach (var item in enSingle) optsList.Add(item?.ToString() ?? "");
                    }

                    int selIdx = ConsoleTui.ShowInteractiveMenu(mTitle, optsList);
                    return (double)selIdx;
                }
                return -1.0;

            case "confirmar":
            case "confirm":
                string cPrompt = evaluatedArgs.Count > 0 ? (evaluatedArgs[0]?.ToString() ?? "") : "Confirmar?";
                bool cDefault = evaluatedArgs.Count > 1 ? Convert.ToBoolean(evaluatedArgs[1]) : true;
                return ConsoleTui.Confirm(cPrompt, cDefault);

            case "caixa":
            case "box":
                if (evaluatedArgs.Count > 0)
                {
                    string bTitle = evaluatedArgs.Count > 1 ? (evaluatedArgs[0]?.ToString() ?? "") : "";
                    string bContent = evaluatedArgs.Count > 1 ? (evaluatedArgs[1]?.ToString() ?? "") : (evaluatedArgs[0]?.ToString() ?? "");
                    ConsoleTui.DrawBox(bTitle, bContent);
                }
                return null;

            case "barra_progresso":
            case "progress_bar":
                if (evaluatedArgs.Count > 0)
                {
                    double prog = Convert.ToDouble(evaluatedArgs[0]);
                    int total = evaluatedArgs.Count > 1 ? Convert.ToInt32(evaluatedArgs[1]) : 100;
                    string label = evaluatedArgs.Count > 2 ? (evaluatedArgs[2]?.ToString() ?? "") : "";
                    ConsoleTui.DrawProgressBar(prog, total, label);
                }
                return null;

            case "tabela":
            case "table":
                if (evaluatedArgs.Count >= 2)
                {
                    var heads = new List<string>();
                    if (evaluatedArgs[0] is YviraList yHeads)
                    {
                        foreach (var h in yHeads.Items) heads.Add(h?.ToString() ?? "");
                    }

                    var tableRows = new List<IReadOnlyList<string>>();
                    if (evaluatedArgs[1] is YviraList yRows)
                    {
                        foreach (var r in yRows.Items)
                        {
                            var rowVals = new List<string>();
                            if (r is YviraList rowList)
                            {
                                foreach (var c in rowList.Items) rowVals.Add(c?.ToString() ?? "");
                            }
                            tableRows.Add(rowVals);
                        }
                    }
                    ConsoleTui.DrawTable(heads, tableRows);
                }
                return null;

            case "limpar_tela":
            case "clear":
                ConsoleTui.Clear();
                return null;

            case "dormir":
            case "sleep":
                if (evaluatedArgs.Count > 0)
                {
                    int ms = Convert.ToInt32(evaluatedArgs[0]);
                    System.Threading.Thread.Sleep(ms);
                }
                return null;

            // --- Motor Gráfico de Janelas Desktop (Yvira GUI Engine) ---
            case "criar_janela":
            case "janela":
            case "window":
                var win = new YviraWindow { InterpreterInstance = this };

                if (evaluatedArgs.Count > 0)
                {
                    Dictionary<string, object?>? configDict = null;
                    if (evaluatedArgs[0] is YviraMap yMap)
                    {
                        configDict = yMap.ToDictionary();
                    }
                    else if (evaluatedArgs[0] is Dictionary<string, object> dObj)
                    {
                        configDict = new Dictionary<string, object?>();
                        foreach (var kvp in dObj) configDict[kvp.Key] = kvp.Value;
                    }
                    else if (evaluatedArgs[0] is Dictionary<string, object?> dObjNull)
                    {
                        configDict = dObjNull;
                    }

                    if (configDict != null)
                    {
                        if (configDict.TryGetValue("titulo", out var tit) || configDict.TryGetValue("title", out tit))
                            win.Title = tit?.ToString() ?? win.Title;

                        if (configDict.TryGetValue("largura", out var w) || configDict.TryGetValue("width", out w))
                            win.Width = Convert.ToInt32(w);

                        if (configDict.TryGetValue("altura", out var h) || configDict.TryGetValue("height", out h))
                            win.Height = Convert.ToInt32(h);

                        if (configDict.TryGetValue("min_largura", out var mw))
                            win.MinWidth = Convert.ToInt32(mw);

                        if (configDict.TryGetValue("min_altura", out var mh))
                            win.MinHeight = Convert.ToInt32(mh);

                        if (configDict.TryGetValue("redimensionavel", out var r) || configDict.TryGetValue("resizable", out r))
                            win.Resizable = Convert.ToBoolean(r);

                        if (configDict.TryGetValue("borda_sistema", out var bs))
                            win.Frameless = !Convert.ToBoolean(bs);
                        else if (configDict.TryGetValue("barra_customizada", out var bc))
                            win.Frameless = Convert.ToBoolean(bc);

                        if (configDict.TryGetValue("cor_fundo", out var bg) || configDict.TryGetValue("fundo", out bg))
                            win.BackgroundColorHex = bg?.ToString() ?? win.BackgroundColorHex;

                        if (configDict.TryGetValue("view", out var v) || configDict.TryGetValue("html", out v) || configDict.TryGetValue("arquivo", out v))
                            win.InitialHtmlPath = v?.ToString();

                        if (configDict.TryGetValue("modo_tela", out var mt) || configDict.TryGetValue("modo", out mt) || configDict.TryGetValue("window_mode", out mt))
                            win.WindowMode = mt?.ToString() ?? win.WindowMode;
                    }
                }

                var winObj = new YviraMap();

                winObj.Set("exibir", new HostFunction("exibir", (interp, a) =>
                {
                    win.Start();
                    return null;
                }));

                winObj.Set("esperar", new HostFunction("esperar", (interp, a) =>
                {
                    win.WaitUntilClosed();
                    return null;
                }));

                winObj.Set("fechar", new HostFunction("fechar", (interp, a) =>
                {
                    win.Close();
                    return null;
                }));

                winObj.Set("ao_evento", new HostFunction("ao_evento", (interp, a) =>
                {
                    if (a.Count >= 2 && a[0] != null && a[1] != null)
                    {
                        string evName = a[0]!.ToString()!;
                        win.On(evName, a[1]!);
                    }
                    return null;
                }));

                winObj.Set("executar_script", new HostFunction("executar_script", (interp, a) =>
                {
                    if (a.Count > 0 && a[0] != null)
                    {
                        win.ExecuteScript(a[0]!.ToString()!);
                    }
                    return null;
                }));

                winObj.Set("enviar_dados", new HostFunction("enviar_dados", (interp, a) =>
                {
                    if (a.Count >= 2 && a[0] != null)
                    {
                        string fnName = a[0]!.ToString()!;
                        object? d = a[1];
                        win.SendData(fnName, d);
                    }
                    return null;
                }));

                // Alias .emitir(evento, dados) que despacha tanto para window[evento](dados) quanto CustomEvent
                winObj.Set("emitir", new HostFunction("emitir", (interp, a) =>
                {
                    if (a.Count >= 1 && a[0] != null)
                    {
                        string fnName = a[0]!.ToString()!;
                        object? d = a.Count > 1 ? a[1] : null;
                        win.SendData(fnName, d);
                    }
                    return null;
                }));

                return winObj;

            case "toNumber":
            case "to_number":
                if (evaluatedArgs.Count == 0 || evaluatedArgs[0] == null) return 0.0;
                if (evaluatedArgs[0] is double dVal) return dVal;
                if (evaluatedArgs[0] is int iVal) return (double)iVal;
                if (evaluatedArgs[0] is long lVal) return (double)lVal;
                string raw = evaluatedArgs[0]!.ToString()!.Trim();
                if (double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedVal))
                {
                    return parsedVal;
                }
                // Tenta com virgula brasileira caso o usuário digite com vírgula no terminal
                if (double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), out double ptVal))
                {
                    return ptVal;
                }
                throw new Exception($"Erro de Execução: Não foi possível converter '{raw}' para número.");

            case "toInt":
            case "to_int":
            case "floor":
                if (evaluatedArgs.Count == 0 || evaluatedArgs[0] == null) return 0.0;
                double numToFloor = Convert.ToDouble(evaluatedArgs[0]);
                return Math.Floor(numToFloor);

            case "round":
                if (evaluatedArgs.Count == 0 || evaluatedArgs[0] == null) return 0.0;
                double numToRound = Convert.ToDouble(evaluatedArgs[0]);
                int decimals = evaluatedArgs.Count > 1 ? Convert.ToInt32(evaluatedArgs[1]) : 0;
                return Math.Round(numToRound, decimals);

            case "toString":
            case "to_string":
                return evaluatedArgs.Count > 0 ? (evaluatedArgs[0]?.ToString() ?? "null") : "";

            case "seed":
            case "random_seed":
                if (evaluatedArgs.Count > 0 && evaluatedArgs[0] != null)
                {
                    int seedVal = Convert.ToInt32(evaluatedArgs[0]);
                    _random = new Random(seedVal);
                }
                return null;

            case "clock":
            case "time":
                return (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            case "random":
                if (evaluatedArgs.Count >= 2)
                {
                    int min = Convert.ToInt32(evaluatedArgs[0]);
                    int max = Convert.ToInt32(evaluatedArgs[1]);
                    return (double)_random.Next(min, max + 1);
                }
                return _random.NextDouble();

            // Geradores Nativos de Matriz
            case "zeros":
                if (evaluatedArgs.Count == 1)
                {
                    int s = Convert.ToInt32(evaluatedArgs[0]);
                    return YviraMatrix.Zeros(s, s);
                }
                if (evaluatedArgs.Count >= 2)
                {
                    int r = Convert.ToInt32(evaluatedArgs[0]);
                    int c = Convert.ToInt32(evaluatedArgs[1]);
                    return YviraMatrix.Zeros(r, c);
                }
                throw new Exception("Erro de Execução: zeros() requer (tamanho) ou (linhas, colunas).");

            case "ones":
                if (evaluatedArgs.Count == 1)
                {
                    int s = Convert.ToInt32(evaluatedArgs[0]);
                    return YviraMatrix.Ones(s, s);
                }
                if (evaluatedArgs.Count >= 2)
                {
                    int r = Convert.ToInt32(evaluatedArgs[0]);
                    int c = Convert.ToInt32(evaluatedArgs[1]);
                    return YviraMatrix.Ones(r, c);
                }
                throw new Exception("Erro de Execução: ones() requer (tamanho) ou (linhas, colunas).");

            case "eye":
            case "identity":
                if (evaluatedArgs.Count >= 1)
                {
                    int s = Convert.ToInt32(evaluatedArgs[0]);
                    return YviraMatrix.Eye(s);
                }
                throw new Exception("Erro de Execução: eye(tamanho) requer a dimensão da matriz identidade.");

            case "randMat":
            case "rand_mat":
                if (evaluatedArgs.Count >= 2)
                {
                    int r = Convert.ToInt32(evaluatedArgs[0]);
                    int c = Convert.ToInt32(evaluatedArgs[1]);
                    double minVal = evaluatedArgs.Count > 2 ? Convert.ToDouble(evaluatedArgs[2]) : 0.0;
                    double maxVal = evaluatedArgs.Count > 3 ? Convert.ToDouble(evaluatedArgs[3]) : 10.0;
                    return YviraMatrix.Random(r, c, minVal, maxVal);
                }
                throw new Exception("Erro de Execução: randMat requer (linhas, colunas, [min], [max]).");

            case "solve":
                if (evaluatedArgs.Count >= 2 && evaluatedArgs[0] is YviraMatrix aMat && evaluatedArgs[1] is YviraMatrix bVec)
                {
                    return aMat.Solve(bVec);
                }
                throw new Exception("Erro de Execução: solve(A, b) requer matriz de coeficientes A e vetor b.");
        }

        // Busca função declarada no ambiente Yvira
        var target = _environment.Get(funcName);
        if (target is YviraClass yvClass)
        {
            return yvClass.Instantiate(this, evaluatedArgs);
        }
        if (target is YviraFunction userFunction)
        {
            return userFunction.Call(this, evaluatedArgs);
        }
        if (target is HostFunction hostFn)
        {
            return hostFn.Call(this, evaluatedArgs);
        }

        throw new Exception($"Erro de Execução: '{funcName}' não é uma função invocável.");
    }

    private object? EvaluateUnary(UnaryExpressionNode un)
    {
        var operand = EvaluateExpression(un.Operand);

        return un.OperatorToken.Type switch
        {
            TokenType.Minus => -(Convert.ToDouble(operand)),
            TokenType.Plus => +(Convert.ToDouble(operand)),
            TokenType.LogicalNot or TokenType.KeywordNot => !IsTruthy(operand),
            _ => throw new Exception($"Operador unário não suportado: {un.OperatorToken.Text}")
        };
    }

    private object? EvaluateBinary(BinaryExpressionNode bin)
    {
        var left = EvaluateExpression(bin.Left);
        var op = bin.OperatorToken.Type;

        // Curto-circuito para operadores lógicos
        if (op == TokenType.LogicalAnd || op == TokenType.KeywordAnd)
        {
            if (!IsTruthy(left)) return false;
            return IsTruthy(EvaluateExpression(bin.Right));
        }

        if (op == TokenType.LogicalOr || op == TokenType.KeywordOr)
        {
            if (IsTruthy(left)) return true;
            return IsTruthy(EvaluateExpression(bin.Right));
        }

        var right = EvaluateExpression(bin.Right);

        return op switch
        {
            // Aritmética
            TokenType.Plus => Add(left, right),
            TokenType.Minus => Subtract(left, right),
            TokenType.Star => Multiply(left, right),
            TokenType.Slash => Divide(Convert.ToDouble(left), Convert.ToDouble(right)),
            TokenType.Modulo or TokenType.Percent => Convert.ToDouble(left) % Convert.ToDouble(right),
            TokenType.Exponent => Math.Pow(Convert.ToDouble(left), Convert.ToDouble(right)),

            // Comparações de igualdade
            TokenType.EqualsEquals => AreEqual(left, right),
            TokenType.NotEquals => !AreEqual(left, right),

            // Comparações relacionais
            TokenType.GreaterThan => Convert.ToDouble(left) > Convert.ToDouble(right),
            TokenType.LessThan => Convert.ToDouble(left) < Convert.ToDouble(right),
            TokenType.GreaterOrEqual => Convert.ToDouble(left) >= Convert.ToDouble(right),
            TokenType.LessOrEqual => Convert.ToDouble(left) <= Convert.ToDouble(right),

            _ => throw new Exception($"Operador binário não suportado: {bin.OperatorToken.Text}")
        };
    }

    private object? EvaluateAssignment(AssignmentExpressionNode assign)
    {
        var targetName = assign.Target.Name;
        var incomingVal = EvaluateExpression(assign.Value);

        object? finalValue = incomingVal;

        if (assign.OperatorToken.Type != TokenType.Assign)
        {
            var currentValue = _environment.Get(targetName);
            finalValue = assign.OperatorToken.Type switch
            {
                TokenType.PlusAssign => Add(currentValue, incomingVal),
                TokenType.MinusAssign => Convert.ToDouble(currentValue) - Convert.ToDouble(incomingVal),
                TokenType.StarAssign => Convert.ToDouble(currentValue) * Convert.ToDouble(incomingVal),
                TokenType.SlashAssign => Divide(Convert.ToDouble(currentValue), Convert.ToDouble(incomingVal)),
                TokenType.PowerAssign => Math.Pow(Convert.ToDouble(currentValue), Convert.ToDouble(incomingVal)),
                _ => throw new Exception($"Operador de atribuição composto não suportado: {assign.OperatorToken.Text}")
            };
        }

        _environment.Assign(targetName, finalValue);
        return finalValue;
    }

    private static object Add(object? left, object? right)
    {
        if (left is YviraMatrix leftMat)
        {
            return leftMat + right!;
        }

        // Concatenação de string se qualquer um for string
        if (left is string || right is string)
        {
            return $"{left}{right}";
        }

        return Convert.ToDouble(left) + Convert.ToDouble(right);
    }

    private static object Subtract(object? left, object? right)
    {
        if (left is YviraMatrix leftMat)
        {
            if (right is YviraMatrix rightMat)
            {
                return leftMat + (rightMat * -1);
            }
            return leftMat + (-Convert.ToDouble(right));
        }

        return Convert.ToDouble(left) - Convert.ToDouble(right);
    }

    private static object Multiply(object? left, object? right)
    {
        if (left is YviraMatrix leftMat)
        {
            return leftMat * right!;
        }

        return Convert.ToDouble(left) * Convert.ToDouble(right);
    }

    private static double Divide(double left, double right)
    {
        if (right == 0.0)
        {
            throw new Exception("Erro de Execução: Divisão por zero.");
        }
        return left / right;
    }

    private static bool AreEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // Se ambos forem numéricos, compara como double
        if (IsNumeric(a) && IsNumeric(b))
        {
            return Convert.ToDouble(a) == Convert.ToDouble(b);
        }

        return a.Equals(b);
    }

    private static bool IsNumeric(object value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    private static bool IsTruthy(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is double d) return d != 0;
        if (value is string s) return s.Length > 0;
        return true;
    }

    private object? EvaluateRealtime(RealtimeExpressionNode node)
    {
        var evaluatedArgs = new List<object?>();
        foreach (var arg in node.Arguments)
        {
            evaluatedArgs.Add(EvaluateExpression(arg));
        }

        string kind = node.Kind.ToLowerInvariant();
        if (kind == "servidor" || kind == "server")
        {
            int porta = 5000;
            string segredo = "";
            int rotacao = 30;

            if (evaluatedArgs.Count > 0)
            {
                if (evaluatedArgs[0] is YviraMap optMap)
                {
                    var p = optMap.Get("porta") ?? optMap.Get("port");
                    if (p != null) porta = Convert.ToInt32(p);

                    var s = optMap.Get("segredo") ?? optMap.Get("secret");
                    if (s != null) segredo = s.ToString() ?? "";

                    var r = optMap.Get("rotacao") ?? optMap.Get("rotacaoSegundos");
                    if (r != null) rotacao = Convert.ToInt32(r);
                }
                else
                {
                    if (evaluatedArgs[0] != null) porta = Convert.ToInt32(evaluatedArgs[0]);
                    if (evaluatedArgs.Count > 1) segredo = evaluatedArgs[1]?.ToString() ?? "";
                    if (evaluatedArgs.Count > 2 && evaluatedArgs[2] != null) rotacao = Convert.ToInt32(evaluatedArgs[2]);
                }
            }

            int limitePacotes = 60;
            if (evaluatedArgs.Count > 0 && evaluatedArgs[0] is YviraMap optMapLimit)
            {
                var lp = optMapLimit.Get("limitePacotes") ?? optMapLimit.Get("maxPackets");
                if (lp != null) limitePacotes = Convert.ToInt32(lp);
            }
            var server = new RealtimeServerInstance(porta, segredo, rotacao, limitePacotes, this);
            var serverMap = new YviraMap();

            serverMap.Set("iniciar", new HostFunction("iniciar", (interp, a) =>
            {
                server.Start();
                return null;
            }));

            serverMap.Set("ao", new HostFunction("ao", (interp, a) =>
            {
                if (a.Count >= 2 && a[0] != null && a[1] != null)
                {
                    server.On(a[0]!.ToString()!, a[1]!);
                }
                return null;
            }));

            serverMap.Set("emitir", new HostFunction("emitir", (interp, a) =>
            {
                if (a.Count >= 1 && a[0] != null)
                {
                    string ev = a[0]!.ToString()!;
                    object? pl = a.Count > 1 ? a[1] : null;
                    server.EmitAll(ev, pl);
                }
                return null;
            }));

            serverMap.Set("para", new HostFunction("para", (interp, a) =>
            {
                string room = a.Count > 0 && a[0] != null ? a[0]!.ToString()! : "";
                var roomScope = new YviraMap();
                roomScope.Set("emitir", new HostFunction("para.emitir", (i, rArgs) =>
                {
                    if (rArgs.Count >= 1 && rArgs[0] != null)
                    {
                        string ev = rArgs[0]!.ToString()!;
                        object? pl = rArgs.Count > 1 ? rArgs[1] : null;
                        server.BroadcastToRoom(room, ev, pl);
                    }
                    return null;
                }));
                return roomScope;
            }));

            serverMap.Set("parar", new HostFunction("parar", (interp, a) =>
            {
                server.Dispose();
                return null;
            }));

            return serverMap;
        }
        else if (kind == "cliente" || kind == "client")
        {
            string url = "ws://localhost:5000";
            string segredo = "";
            int rotacao = 30;

            if (evaluatedArgs.Count > 0)
            {
                if (evaluatedArgs[0] is YviraMap optMap)
                {
                    var u = optMap.Get("endereco") ?? optMap.Get("url");
                    if (u != null) url = u.ToString() ?? url;

                    var s = optMap.Get("segredo") ?? optMap.Get("secret");
                    if (s != null) segredo = s.ToString() ?? "";

                    var r = optMap.Get("rotacao") ?? optMap.Get("rotacaoSegundos");
                    if (r != null) rotacao = Convert.ToInt32(r);
                }
                else
                {
                    url = evaluatedArgs[0]?.ToString() ?? url;
                    if (evaluatedArgs.Count > 1) segredo = evaluatedArgs[1]?.ToString() ?? "";
                    if (evaluatedArgs.Count > 2 && evaluatedArgs[2] != null) rotacao = Convert.ToInt32(evaluatedArgs[2]);
                }
            }

            var client = new RealtimeClientInstance(url, segredo, rotacao, this);
            var clientMap = new YviraMap();

            clientMap.Set("conectar", new HostFunction("conectar", (interp, a) =>
            {
                client.Connect();
                return null;
            }));

            clientMap.Set("ao", new HostFunction("ao", (interp, a) =>
            {
                if (a.Count >= 2 && a[0] != null && a[1] != null)
                {
                    client.On(a[0]!.ToString()!, a[1]!);
                }
                return null;
            }));

            clientMap.Set("emitir", new HostFunction("emitir", (interp, a) =>
            {
                if (a.Count >= 1 && a[0] != null)
                {
                    string ev = a[0]!.ToString()!;
                    object? pl = a.Count > 1 ? a[1] : null;
                    _ = client.EmitAsync(ev, pl);
                }
                return null;
            }));

            clientMap.Set("desconectar", new HostFunction("desconectar", (interp, a) =>
            {
                client.Dispose();
                return null;
            }));

            return clientMap;
        }
        else if (kind == "descobrir" || kind == "discovery" || kind == "localizar")
        {
            int timeoutMs = 3000;
            if (evaluatedArgs.Count > 0 && evaluatedArgs[0] != null)
            {
                timeoutMs = Convert.ToInt32(Convert.ToDouble(evaluatedArgs[0]) * 1000);
            }

            var discoveryTask = RealtimeDiscovery.DiscoverServerAsync(timeoutMs);
            var res = discoveryTask.GetAwaiter().GetResult();

            if (res != null)
            {
                var resMap = new YviraMap();
                resMap.Set("encontrado", true);
                resMap.Set("ip", res.Ip);
                resMap.Set("porta", (double)res.Port);
                resMap.Set("endereco", res.Url);
                resMap.Set("url", res.Url);
                resMap.Set("nome", res.Name);
                resMap.Set("segredo", res.Secret);
                return resMap;
            }

            var nullMap = new YviraMap();
            nullMap.Set("encontrado", false);
            nullMap.Set("endereco", "ws://localhost:7070");
            nullMap.Set("url", "ws://localhost:7070");
            return nullMap;
        }

        return null;
    }

    private object? ExecuteMatchStatement(MatchStatementNode matchStmt)
    {
        var targetValue = EvaluateExpression(matchStmt.Expression);

        foreach (var arm in matchStmt.Arms)
        {
            bool matched = false;
            if (arm.IsWildcard)
            {
                matched = true;
            }
            else if (arm.Pattern != null)
            {
                var patternVal = EvaluateExpression(arm.Pattern);
                matched = AreEqual(targetValue, patternVal);
            }

            if (matched)
            {
                if (arm.Body is BlockStatementNode block)
                {
                    ExecuteBlock(block);
                    return null;
                }
                else if (arm.Body is StatementNode stmt)
                {
                    return ExecuteStatement(stmt);
                }
                else if (arm.Body is ExpressionNode expr)
                {
                    return EvaluateExpression(expr);
                }
                break;
            }
        }
        return null;
    }

    private void ExecuteDieStatement(DieStatementNode dieStmt)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n======================== [@die INTERRUPÇÃO DE DEBUG] ========================");
        Console.ResetColor();

        if (dieStmt.Arguments.Count > 0)
        {
            for (int i = 0; i < dieStmt.Arguments.Count; i++)
            {
                var val = EvaluateExpression(dieStmt.Arguments[i]);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"[{i}]: ");
                Console.ResetColor();
                Console.WriteLine((val != null ? val.ToString() : "null"));
            }
        }
        else
        {
            Console.WriteLine("[@die chamado sem argumentos. Execucao interrompida]");
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("-----------------------------------------------------------------------------");
        Console.WriteLine($"Diretorio Atual: {_currentDirectory}");
        Console.WriteLine($"Timestamp      : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        Console.WriteLine("=============================================================================\n");
        Console.ResetColor();

        throw new YviraRuntimeException("@die executado: processo finalizado sob demanda pelo desenvolvedor.");
    }

    private void ExecuteSleepStatement(SleepStatementNode sleepStmt)
    {
        var durVal = EvaluateExpression(sleepStmt.Duration);
        double seconds = 1.0;
        if (durVal is double d) seconds = d;
        else if (durVal is int i) seconds = i;
        else if (durVal is long l) seconds = l;
        else if (double.TryParse(durVal?.ToString(), out double parsed)) seconds = parsed;

        if (seconds > 0)
        {
            System.Threading.Thread.Sleep((int)(seconds * 1000));
        }
    }

    private string EvaluateInterpolatedString(InterpolatedStringNode interp)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < interp.StringParts.Count; i++)
        {
            sb.Append(interp.StringParts[i]);
            if (i < interp.Expressions.Count)
            {
                var val = EvaluateExpression(interp.Expressions[i]);
                sb.Append((val != null ? val.ToString() : "null"));
            }
        }
        return sb.ToString();
    }

    private void ExecutePdfDirective(PdfDirectiveStatementNode pdfStmt)
    {
        string rawFilePath = EvaluateExpression(pdfStmt.FilePathExpression)?.ToString() ?? "documento.pdf";
        string fullPath = System.IO.Path.IsPathRooted(rawFilePath)
            ? rawFilePath
            : System.IO.Path.GetFullPath(System.IO.Path.Combine(_currentDirectory, rawFilePath));

        string? dir = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        // Caso @pdf.fromView("template.html", "arquivo.pdf", [dados])
        if (pdfStmt.Method != null && pdfStmt.Method.Equals("fromView", StringComparison.OrdinalIgnoreCase))
        {
            string viewFile = rawFilePath;
            string targetPdf = pdfStmt.Arguments.Count > 0 ? (EvaluateExpression(pdfStmt.Arguments[0])?.ToString() ?? "view.pdf") : "view.pdf";
            string fullViewPath = System.IO.Path.IsPathRooted(viewFile) ? viewFile : System.IO.Path.GetFullPath(System.IO.Path.Combine(_currentDirectory, viewFile));
            string fullTargetPdf = System.IO.Path.IsPathRooted(targetPdf) ? targetPdf : System.IO.Path.GetFullPath(System.IO.Path.Combine(_currentDirectory, targetPdf));

            if (!System.IO.File.Exists(fullViewPath))
                throw new System.IO.FileNotFoundException($"Arquivo de template view não encontrado: {fullViewPath}");

            string html = System.IO.File.ReadAllText(fullViewPath);
            if (pdfStmt.Arguments.Count > 1 && EvaluateExpression(pdfStmt.Arguments[1]) is YviraMap dataMap)
            {
                foreach (var entry in dataMap)
                {
                    html = html.Replace("{{" + entry.Key + "}}", entry.Value?.ToString() ?? "");
                }
            }

            var ctxView = new PdfBuilderContext { Title = System.IO.Path.GetFileNameWithoutExtension(fullViewPath) };
            ctxView.Sections.Add(new PdfBuilderContext.ViewSection { HtmlContent = html });
            DocumentModule.RenderPdfContext(ctxView, fullTargetPdf);
            return;
        }

        // Caso @pdf("arquivo.pdf", [opcoes]) { title(...); subtitle(...); table(...); text(...); footer(...); }
        var ctx = new PdfBuilderContext();
        if (pdfStmt.Arguments.Count > 0)
        {
            var optObj = EvaluateExpression(pdfStmt.Arguments[0]);
            if (optObj is YviraMap optMap)
            {
                ctx.PageSize = optMap.Get("size")?.ToString() ?? ctx.PageSize;
                ctx.Orientation = optMap.Get("orientation")?.ToString() ?? ctx.Orientation;
            }
        }

        if (pdfStmt.Body != null)
        {
            var pdfEnv = new Environment(_environment);
            
            // Registra DSL interna do bloco @pdf
            pdfEnv.Define("title", new HostFunction("title", (interp, args) =>
            {
                if (args.Count > 0 && args[0] != null) ctx.Title = args[0]!.ToString()!;
                if (args.Count > 1 && args[1] is YviraMap opts)
                {
                    ctx.TitleColor = opts.Get("color")?.ToString() ?? ctx.TitleColor;
                    ctx.TitleAlign = opts.Get("align")?.ToString() ?? ctx.TitleAlign;
                }
                return null;
            }));

            pdfEnv.Define("subtitle", new HostFunction("subtitle", (interp, args) =>
            {
                if (args.Count > 0 && args[0] != null) ctx.Subtitle = args[0]!.ToString()!;
                return null;
            }));

            pdfEnv.Define("text", new HostFunction("text", (interp, args) =>
            {
                if (args.Count > 0 && args[0] != null)
                {
                    var textSec = new PdfBuilderContext.TextSection { Text = args[0]!.ToString()! };
                    if (args.Count > 1 && args[1] is YviraMap opts)
                    {
                        textSec.Color = opts.Get("color")?.ToString();
                        textSec.Align = opts.Get("align")?.ToString() ?? "left";
                        if (opts.Get("size") is double sz) textSec.FontSize = (float)sz;
                        if (opts.Get("bold") is bool b) textSec.Bold = b;
                        if (opts.Get("italic") is bool it) textSec.Italic = it;
                    }
                    ctx.Sections.Add(textSec);
                }
                return null;
            }));

            // Ingestão Polimórfica Direta: table(vendas, [opts])
            pdfEnv.Define("table", new HostFunction("table", (interp, args) =>
            {
                if (args.Count > 0 && args[0] != null)
                {
                    DocumentModule.ExtractTableData(args[0]!, out var autoHeaders, out var autoRows);

                    var tableSec = new PdfBuilderContext.TableSection
                    {
                        Headers = autoHeaders,
                        Rows = autoRows
                    };

                    if (args.Count > 1 && args[1] is YviraMap opts)
                    {
                        if (opts.Get("headers") is YviraList customHeaders)
                        {
                            tableSec.Headers = customHeaders.Select(h => h?.ToString() ?? "").ToList();
                        }
                        tableSec.Theme = opts.Get("theme")?.ToString() ?? "striped";
                        tableSec.HeaderColor = opts.Get("color")?.ToString() ?? opts.Get("headerColor")?.ToString();
                    }

                    ctx.Sections.Add(tableSec);
                }
                return null;
            }));

            pdfEnv.Define("footer", new HostFunction("footer", (interp, args) =>
            {
                if (args.Count > 0 && args[0] != null) ctx.Footer = args[0]!.ToString()!;
                return null;
            }));

            pdfEnv.Define("view", new HostFunction("view", (interp, args) =>
            {
                if (args.Count > 0 && args[0] != null)
                {
                    string vPath = args[0]!.ToString()!;
                    string fvPath = System.IO.Path.IsPathRooted(vPath) ? vPath : System.IO.Path.GetFullPath(System.IO.Path.Combine(_currentDirectory, vPath));
                    if (System.IO.File.Exists(fvPath))
                    {
                        string hContent = System.IO.File.ReadAllText(fvPath);
                        if (args.Count > 1 && args[1] is YviraMap dataMap)
                        {
                            foreach (var entry in dataMap)
                            {
                                hContent = hContent.Replace("{{" + entry.Key + "}}", entry.Value?.ToString() ?? "");
                            }
                        }
                        ctx.Sections.Add(new PdfBuilderContext.ViewSection { HtmlContent = hContent });
                    }
                }
                return null;
            }));

            var prevEnv = _environment;
            _environment = pdfEnv;
            try
            {
                ExecuteBlock(pdfStmt.Body);
            }
            finally
            {
                _environment = prevEnv;
            }
        }

        DocumentModule.RenderPdfContext(ctx, fullPath);
    }

}