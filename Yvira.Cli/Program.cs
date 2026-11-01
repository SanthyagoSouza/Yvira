using System;
using System.IO;
using Yvira.Cli;
using Yvira.Parser;
using Yvira.Vm;

class Program
{
    static int Main(string[] args)
    {
        // 0. Verifica se este executavel e uma aplicacao selada (Single-File Bundle)
        string? sealedEntry = AppBundler.CheckAndExtractSealedBundle();
        if (!string.IsNullOrEmpty(sealedEntry) && File.Exists(sealedEntry))
        {
            return RunFile(sealedEntry, args);
        }

        if (args.Length == 0)
        {
            var manifest = ProjectManifest.LoadFromCurrentDirectory(Directory.GetCurrentDirectory());
            if (manifest != null && File.Exists(manifest.Entry))
            {
                return RunFile(manifest.Entry, Array.Empty<string>());
            }

            return RunInteractiveMenu();
        }

        string command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "build":
            case "package":
            case "bundle":
                return AppBundler.Build(args);

            case "menu":
            case "ui":
                return RunInteractiveMenu();

            case "upgrade":
            case "update":
                return Updater.RunUpgradeAsync().GetAwaiter().GetResult();

            case "install":
            case "i":
            case "add":
                return RunInstall(args);

            case "init":
            {
                string projName = args.Length >= 2 ? args[1] : Path.GetFileName(Directory.GetCurrentDirectory());
                string targetDir = args.Length >= 2 ? Path.Combine(Directory.GetCurrentDirectory(), args[1]) : Directory.GetCurrentDirectory();
                ProjectManifest.CreateNewProject(targetDir, projName);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[Sucesso] Projeto '{projName}' inicializado com sucesso!");
                Console.ResetColor();
                Console.WriteLine("Arquivos criados:");
                Console.WriteLine("  - yvira.json   (Manifesto do projeto)");
                Console.WriteLine("  - main.yv      (Ponto de entrada do codigo)");
                Console.WriteLine("  - .env         (Variaveis de ambiente)");
                Console.WriteLine("  - .gitignore   (Padrao para logs e temporarios)\n");
                Console.WriteLine("Para executar:");
                if (args.Length >= 2) Console.WriteLine($"  cd {projName}");
                Console.WriteLine("  yvira run\n");
                return 0;
            }

            case "info":
            {
                PrintBanner();
                var currentProj = ProjectManifest.LoadFromCurrentDirectory(Directory.GetCurrentDirectory());
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Informacoes do Ambiente Yvira:");
                Console.ResetColor();
                Console.WriteLine($"  Versao do Yvira : {Updater.CurrentVersion}");
                Console.WriteLine($"  Runtime .NET    : {System.Environment.Version}");
                Console.WriteLine($"  Diretorio Atual : {Directory.GetCurrentDirectory()}");
                if (currentProj != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nProjeto Ativo (yvira.json):");
                    Console.ResetColor();
                    Console.WriteLine($"  Nome        : {currentProj.Name}");
                    Console.WriteLine($"  Versao      : {currentProj.Version}");
                    Console.WriteLine($"  Descricao   : {currentProj.Description}");
                    Console.WriteLine($"  Entrypoint  : {currentProj.Entry}");
                    Console.WriteLine($"  Autor       : {currentProj.Author}");
                }
                else
                {
                    Console.WriteLine("\nNenhum projeto 'yvira.json' detectado nesta pasta.");
                }
                Console.WriteLine();
                return 0;
            }

            case "bench":
                if (args.Length < 2)
                {
                    Console.WriteLine("Uso: yvira bench <arquivo.yv>");
                    return 1;
                }
                return RunBenchmark(args[1]);

            case "vm":
                if (args.Length < 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Erro: Especifique o arquivo para executar via VM.");
                    Console.ResetColor();
                    return 1;
                }
                return RunBytecodeVm(args[1]);

            case "lsp-check":
                if (args.Length < 2) return 1;
                return RunLspCheck(args[1]);

            case "run":
                if (args.Length < 2)
                {
                    var m = ProjectManifest.LoadFromCurrentDirectory(Directory.GetCurrentDirectory());
                    if (m != null && File.Exists(m.Entry))
                    {
                        var restArgs = args.Skip(1).ToArray();
                    return RunFile(m.Entry, restArgs);
                    }
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Erro: Especifique o arquivo a ser executado ou execute dentro de um projeto com 'yvira.json'.");
                    Console.ResetColor();
                    return 1;
                }
                return RunFile(args[1], args.Skip(2).ToArray());

            case "repl":
                return RunRepl();

            case "test":
                RunTests();
                return 0;

            default:
                if (File.Exists(args[0]))
                {
                    return RunFile(args[0], args.Skip(1).ToArray());
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Comando desconhecido: '{args[0]}'");
                Console.ResetColor();
                return 1;
        }
    }

    static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
 __     __      _             
 \ \   / /     (_)            
  \ \_/ /__   ___ _ __ __ _   
   \   / \ \ / / | '__/ _` |  
    | |   \ V /| | | | (_| |  
    |_|    \_/ |_|_|  \__,_|  
                              
 Sistema Operacional & Linguagem Cientifica Beta
");
        Console.ResetColor();
    }

    static int RunBenchmark(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Arquivo não encontrado: {filePath}");
            return 1;
        }

        string source = File.ReadAllText(filePath);

        // 1. AST
        var p1 = new Parser(source);
        var ast1 = p1.ParseProgram();
        var interp = new Interpreter(outputHandler: _ => { });
        var swAst = System.Diagnostics.Stopwatch.StartNew();
        interp.Execute(ast1);
        swAst.Stop();

        // 2. VM
        var p2 = new Parser(source);
        var ast2 = p2.ParseProgram();
        var compiler = new Yvira.Vm.Bytecode.Compiler();
        var compiledFn = compiler.Compile(ast2);
        var vm = new Yvira.Vm.Bytecode.VirtualMachine(outputHandler: _ => { });
        var swVm = System.Diagnostics.Stopwatch.StartNew();
        vm.Execute(compiledFn);
        swVm.Stop();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n=== RESULTADO DO BENCHMARK DE PERFORMANCE ===");
        Console.ResetColor();
        Console.WriteLine($"[1] Interpretador AST (Tree-walk)  : {swAst.ElapsedMilliseconds} ms");
        Console.WriteLine($"[2] Compilador Bytecode & Stack VM  : {swVm.ElapsedMilliseconds} ms");
        if (swVm.ElapsedMilliseconds > 0)
        {
            double speedup = (double)swAst.ElapsedMilliseconds / swVm.ElapsedMilliseconds;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Aceleração Obtida                   : {speedup:F1}x mais rápido!");
            Console.ResetColor();
        }
        return 0;
    }

    static int RunLspCheck(string filePath)
    {
        if (!File.Exists(filePath)) return 1;
        string source = File.ReadAllText(filePath);
        var parser = new Parser(source);
        parser.ParseProgram();

        var errors = new System.Collections.Generic.List<object>();
        foreach (var diag in parser.Diagnostics)
        {
            errors.Add(new { message = diag, line = 1, column = 1 });
        }

        string json = System.Text.Json.JsonSerializer.Serialize(errors);
        Console.WriteLine(json);
        return 0;
    }

    static int RunBytecodeVm(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Erro: Arquivo não encontrado: {filePath}");
            Console.ResetColor();
            return 1;
        }

        string source = File.ReadAllText(filePath);
        var parser = new Parser(source);
        var ast = parser.ParseProgram();

        if (parser.Diagnostics.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Erros de compilação detectados:");
            foreach (var diag in parser.Diagnostics) Console.WriteLine($"  * {diag}");
            Console.ResetColor();
            return 1;
        }

        try
        {
            var compiler = new Yvira.Vm.Bytecode.Compiler();
            var compiledFn = compiler.Compile(ast);

            var vm = new Yvira.Vm.Bytecode.VirtualMachine();
            vm.Execute(compiledFn);
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Erro na Máquina Virtual (Bytecode VM): {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static int RunFile(string filePath, string[]? scriptArgs = null)
    {
        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Erro: Arquivo não encontrado: {filePath}");
            Console.ResetColor();
            return 1;
        }

        string source = File.ReadAllText(filePath);
        string? dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        string fullPath = Path.GetFullPath(filePath);
        return ExecuteSource(source, dir, fullPath, scriptArgs);
    }

    static Yvira.Ast.ProgramNode ParseOrThrow(string src)
    {
        var p = new Parser(src);
        var prog = p.ParseProgram();
        if (p.Diagnostics.Count > 0)
        {
            throw new Exception(string.Join("; ", p.Diagnostics));
        }
        return prog;
    }

    static int ExecuteSource(string source, string? workingDir = null, string? mainFilePath = null, string[]? scriptArgs = null)
    {
        var parser = new Parser(source);
        var ast = parser.ParseProgram();

        if (parser.Diagnostics.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Erros de compilação detectados:");
            foreach (var diag in parser.Diagnostics)
            {
                Console.WriteLine($"  * {diag}");
            }
            Console.ResetColor();
            return 1;
        }

        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (mainFilePath != null) loaded.Add(mainFilePath);
        var interpreter = new Interpreter(currentDirectory: workingDir, loadedModules: loaded, moduleLoader: ParseOrThrow, scriptArgs: scriptArgs);
        interpreter.Execute(ast);

        // Se houver tarefas de agendamento ativas (@time), mantem o processo vivo ate que sejam finalizadas ou receba Ctrl+C
        bool hasHttpServers = interpreter.HttpServers.Any(s => s.IsRunning);
        if (interpreter.Scheduler.HasActiveTasks || hasHttpServers)
        {
            var cts = new System.Threading.CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                interpreter.Scheduler.StopAll();
                interpreter.StopAllHttpServers();
            };

            while ((interpreter.Scheduler.HasActiveTasks || interpreter.HttpServers.Any(s => s.IsRunning)) && !cts.IsCancellationRequested)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        if (interpreter.RuntimeErrors.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Erros de execução detectados:");
            foreach (var err in interpreter.RuntimeErrors)
            {
                Console.WriteLine($"  * {err}");
            }
            Console.ResetColor();
            return 1;
        }

        return 0;
    }

    static int RunRepl()
    {
        PrintBanner();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  Yvira Interativo (REPL v2.1) - Destaque ao vivo & Historico com setas");
        Console.ResetColor();
        Console.WriteLine("  Digite 'exit', 'sair' ou pressione Ctrl+C para encerrar.\n");

        var interpreter = new Interpreter(moduleLoader: ParseOrThrow);

        while (true)
        {
            string? firstLine = ReplLineReader.ReadLineWithHighlight("yvira> ");
            if (firstLine == null || firstLine.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) || firstLine.Trim().Equals("sair", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(firstLine))
            {
                continue;
            }

            var sb = new System.Text.StringBuilder(firstLine);

            while (HasOpenDelimiters(sb.ToString()))
            {
                string? nextLine = ReplLineReader.ReadLineWithHighlight("...   ", isContinuation: true);
                if (nextLine == null) break;
                sb.AppendLine();
                sb.Append(nextLine);
            }

            string fullCode = sb.ToString();

            if (!fullCode.TrimEnd().EndsWith(";") && !fullCode.TrimEnd().EndsWith("}"))
            {
                fullCode += ";";
            }

            var parser = new Parser(fullCode);
            var ast = parser.ParseProgram();

            if (parser.Diagnostics.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var diag in parser.Diagnostics)
                {
                    Console.WriteLine($"  Erro sintatico: {diag}");
                }
                Console.ResetColor();
                continue;
            }

            var result = interpreter.Execute(ast);
            if (interpreter.RuntimeErrors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var err in interpreter.RuntimeErrors)
                {
                    Console.WriteLine($"  Erro de execucao: {err}");
                }
                Console.ResetColor();
            }
            else if (result != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"=> {result}");
                Console.ResetColor();
            }
        }

        return 0;
    }

    private static bool HasOpenDelimiters(string text)
    {
        int braces = 0;
        int parens = 0;
        int brackets = 0;
        bool inString = false;
        char quote = '\0';

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == '#' && !inString) break;

            if (!inString && (ch == '"' || ch == '\''))
            {
                inString = true;
                quote = ch;
                continue;
            }

            if (inString && ch == quote && (i == 0 || text[i - 1] != '\\'))
            {
                inString = false;
                continue;
            }

            if (!inString)
            {
                if (ch == '{') braces++;
                else if (ch == '}') braces = Math.Max(0, braces - 1);
                else if (ch == '(') parens++;
                else if (ch == ')') parens = Math.Max(0, parens - 1);
                else if (ch == '[') brackets++;
                else if (ch == ']') brackets = Math.Max(0, brackets - 1);
            }
        }

        return braces > 0 || parens > 0 || brackets > 0 || inString;
    }

    static void RunTests()
    {
        Console.WriteLine("Executando bateria de testes...\n");
        string testScript = @"
            print(""Iniciando script de validacao..."");
            let x = 10;
            let y = 20;
            let total = x * 2 + y;
            print(""Resultado da conta:"", total);
            print(""Yvira rodando com sucesso!"");
        ";
        ExecuteSource(testScript);
    }

    static int RunInstall(string[] args)
    {
        string currentDir = Directory.GetCurrentDirectory();
        var manifest = ProjectManifest.LoadFromCurrentDirectory(currentDir);
        if (manifest == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Aviso: Nenhum 'yvira.json' encontrado neste diretorio. Criando um novo...");
            Console.ResetColor();
            ProjectManifest.CreateNewProject(currentDir, Path.GetFileName(currentDir));
            manifest = ProjectManifest.LoadFromCurrentDirectory(currentDir)!;
        }

        // Se passou argumento específico: yvira install <alias>=<origem> ou yvira install <origem>
        if (args.Length >= 2)
        {
            string targetSpec = args[1];
            string alias = "";
            string source = "";

            if (targetSpec.Contains("="))
            {
                var parts = targetSpec.Split('=', 2);
                alias = parts[0];
                source = parts[1];
            }
            else
            {
                source = targetSpec;
                // Infere o alias
                if (source.StartsWith("github:", StringComparison.OrdinalIgnoreCase))
                {
                    string repo = source.Substring("github:".Length).Split('@')[0];
                    alias = Path.GetFileName(repo);
                }
                else if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    string rawName = Path.GetFileNameWithoutExtension(new Uri(source).AbsolutePath);
                    alias = string.IsNullOrWhiteSpace(rawName) ? "pacote" : rawName;
                }
                else
                {
                    alias = targetSpec;
                    source = targetSpec;
                }
            }

            try
            {
                PackageManager.InstallDependencyAsync(currentDir, alias, source).GetAwaiter().GetResult();
                manifest.Dependencies[alias] = source;
                manifest.Save(currentDir);
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Erro de Instalacao]: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        // Sem argumento específico: instala todas as dependências declaradas no yvira.json
        if (manifest.Dependencies.Count == 0)
        {
            Console.WriteLine("Nenhuma dependencia declarada no 'yvira.json'.");
            return 0;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Restaurando {manifest.Dependencies.Count} dependencias declaradas no yvira.json...\n");
        Console.ResetColor();

        int successCount = 0;
        foreach (var dep in manifest.Dependencies)
        {
            try
            {
                PackageManager.InstallDependencyAsync(currentDir, dep.Key, dep.Value).GetAwaiter().GetResult();
                successCount++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Falha ao instalar {dep.Key}]: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nConcluido: {successCount}/{manifest.Dependencies.Count} pacotes instalados com sucesso!");
        Console.ResetColor();
        return 0;
    }

    private static int RunInteractiveMenu()
    {
        PrintBanner();

        var options = new List<string>
        {
            "1. Executar Script (.yv)",
            "2. Iniciar Console Interativo (REPL)",
            "3. Inicializar Novo Projeto (yvira init)",
            "4. Informações do Ambiente e Sistema (yvira info)",
            "5. Executar Bateria de Testes (yvira test)",
            "6. Atualizar Yvira (yvira upgrade)",
            "0. Sair"
        };

        int choice = ConsoleTui.ShowInteractiveMenu("CENTRAL DE COMANDOS YVIRA", options);

        switch (choice)
        {
            case 0:
            {
                // Buscar arquivos .yv na pasta atual
                var yvFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.yv", SearchOption.TopDirectoryOnly);
                if (yvFiles.Length == 0)
                {
                    Console.Write("\nDigite o caminho do arquivo .yv: ");
                    string? path = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        return RunFile(path);
                    }
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Arquivo não encontrado!");
                    Console.ResetColor();
                    return 1;
                }

                var fileOptions = new List<string>();
                foreach (var f in yvFiles)
                {
                    fileOptions.Add(Path.GetFileName(f));
                }
                fileOptions.Add("[ Digitar outro caminho... ]");
                fileOptions.Add("[ Cancelar ]");

                int fChoice = ConsoleTui.ShowInteractiveMenu("Selecione o arquivo para executar", fileOptions);
                if (fChoice >= 0 && fChoice < yvFiles.Length)
                {
                    return RunFile(yvFiles[fChoice]);
                }
                else if (fChoice == yvFiles.Length)
                {
                    Console.Write("\nDigite o caminho do arquivo: ");
                    string? custom = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(custom) && File.Exists(custom))
                    {
                        return RunFile(custom);
                    }
                }
                return 0;
            }

            case 1:
                return RunRepl();

            case 2:
            {
                Console.Write("\nNome do novo projeto: ");
                string? pName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(pName)) pName = "meu_projeto";
                string targetDir = Path.Combine(Directory.GetCurrentDirectory(), pName);
                ProjectManifest.CreateNewProject(targetDir, pName);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[Sucesso] Projeto '{pName}' criado com sucesso!");
                Console.ResetColor();
                return 0;
            }

            case 3:
            {
                var manifest = ProjectManifest.LoadFromCurrentDirectory(Directory.GetCurrentDirectory());
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\nInformacoes do Ambiente Yvira:");
                Console.ResetColor();
                Console.WriteLine($"  Versao do Yvira : {Updater.CurrentVersion}");
                Console.WriteLine($"  Runtime .NET    : {System.Environment.Version}");
                Console.WriteLine($"  Diretorio Atual : {Directory.GetCurrentDirectory()}");
                if (manifest != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nProjeto Ativo (yvira.json):");
                    Console.ResetColor();
                    Console.WriteLine($"  Nome        : {manifest.Name}");
                    Console.WriteLine($"  Versao      : {manifest.Version}");
                    Console.WriteLine($"  Descricao   : {manifest.Description}");
                    Console.WriteLine($"  Entrypoint  : {manifest.Entry}");
                }
                Console.WriteLine();
                return 0;
            }

            case 4:
                RunTests();
                return 0;

            case 5:
                return Updater.RunUpgradeAsync().GetAwaiter().GetResult();

            default:
                Console.WriteLine("Até logo!");
                return 0;
        }
    }
}
