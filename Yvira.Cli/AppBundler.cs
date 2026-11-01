using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Yvira.Cli
{
    /// <summary>
    /// Motor de Compilação e Empacotamento de Executáveis Nativos Independentes Yvira.
    /// Gera aplicações portáteis (.exe) contendo o runtime e os assets do projeto embutidos.
    /// </summary>
    public static class AppBundler
    {
        // Assinatura de 16 bytes no final do executável para marcar um bundle selado Yvira
        private static readonly byte[] BundleMagic = Encoding.ASCII.GetBytes("YVIRA_BUNDLE_V1!");

        public static int Build(string[] args)
        {
            string currentDir = Directory.GetCurrentDirectory();
            var manifest = ProjectManifest.LoadFromCurrentDirectory(currentDir);

            string appName = manifest?.Name ?? Path.GetFileName(currentDir);
            string entryFile = manifest?.Entry ?? "main.yv";

            bool singleFile = false;
            string outputDir = Path.Combine(currentDir, "dist");

            // Processa argumentos: yvira build [--single-file] [-o MeuApp]
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();
                if (arg == "--single-file" || arg == "-s" || arg == "--sealed")
                {
                    singleFile = true;
                }
                else if ((arg == "-o" || arg == "--output") && i + 1 < args.Length)
                {
                    appName = args[++i];
                }
                else if (arg == "--dir" && i + 1 < args.Length)
                {
                    outputDir = args[++i];
                }
                else if (File.Exists(args[i]) && args[i].EndsWith(".yv"))
                {
                    entryFile = args[i];
                }
            }

            if (!File.Exists(Path.Combine(currentDir, entryFile)))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Erro de Build]: Arquivo de entrada '{entryFile}' nao foi encontrado.");
                Console.ResetColor();
                return 1;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=================================================");
            Console.WriteLine($"   YVIRA COMPILER & APP BUNDLER - {appName.ToUpper()}");
            Console.WriteLine("=================================================");
            Console.ResetColor();
            Console.WriteLine($"  Projeto      : {appName}");
            Console.WriteLine($"  Ponto Entrada: {entryFile}");
            Console.WriteLine($"  Modo         : {(singleFile ? "Single-File Selado (1 arquivo .exe)" : "Standalone + Views (Executavel Portatil)")}");
            Console.WriteLine($"  Saida        : {outputDir}\n");

            // Localiza o executável base do Yvira
            string currentExe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "yvira.exe");
            if (!File.Exists(currentExe))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Erro]: Binario base do motor Yvira nao encontrado em '{currentExe}'.");
                Console.ResetColor();
                return 1;
            }

            Directory.CreateDirectory(outputDir);
            string cleanAppName = appName.Replace(" ", "_");
            string targetExe = Path.Combine(outputDir, cleanAppName + ".exe");

            if (singleFile)
            {
                return BuildSingleFileSealed(currentDir, entryFile, currentExe, targetExe, cleanAppName);
            }
            else
            {
                return BuildPortableDirectory(currentDir, entryFile, currentExe, targetExe, outputDir, cleanAppName);
            }
        }

        /// <summary>
        /// OPÇÃO 1: Executável Portátil Standalone + pasta de Views/Assets (Estilo Electron / Steam Game)
        /// </summary>
        private static int BuildPortableDirectory(string currentDir, string entryFile, string currentExe, string targetExe, string outputDir, string appName)
        {
            Console.WriteLine("[1/3] Gerando executavel standalone personalizado...");
            File.Copy(currentExe, targetExe, true);

            // Garante que o yvira.json aponte para o entryFile correto
            var manifest = ProjectManifest.LoadFromCurrentDirectory(currentDir) ?? new ProjectManifest
            {
                Name = appName,
                Entry = entryFile
            };
            manifest.Entry = entryFile;
            manifest.Save(outputDir);

            Console.WriteLine("[2/3] Copiando codigo-fonte Yvira e dependencias...");
            // Copia o entryFile
            string targetEntry = Path.Combine(outputDir, entryFile);
            string? entrySubDir = Path.GetDirectoryName(targetEntry);
            if (!string.IsNullOrEmpty(entrySubDir)) Directory.CreateDirectory(entrySubDir);
            File.Copy(Path.Combine(currentDir, entryFile), targetEntry, true);

            // Copia subdiretórios essenciais se existirem (src, views, assets, dados)
            string[] dirsToCopy = { "src", "views", "assets", "dados" };
            foreach (var d in dirsToCopy)
            {
                string sourceSub = Path.Combine(currentDir, d);
                if (Directory.Exists(sourceSub))
                {
                    CopyDirectory(sourceSub, Path.Combine(outputDir, d));
                }
            }

            Console.WriteLine("[3/3] Finalizando pacote standalone...");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[SUCESSO] Aplicacao gerada com sucesso!");
            Console.ResetColor();
            Console.WriteLine($"  Executavel: {targetExe}");
            Console.WriteLine($"  Basta compactar a pasta '{outputDir}' ou distribuir para qualquer maquina Windows.");
            Console.WriteLine("  O usuário apenas da 2 cliques no executavel e o programa abre na hora!\n");
            return 0;
        }

        /// <summary>
        /// OPÇÃO 2: Single-File Executable 100% Selado (1 arquivo .exe único contendo tudo)
        /// </summary>
        private static int BuildSingleFileSealed(string currentDir, string entryFile, string currentExe, string targetExe, string appName)
        {
            Console.WriteLine("[1/3] Criando arquivo ZIP em memoria com todos os codigos e assets...");
            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                // Inclui arquivos .yv e manifesto
                AddFileToArchive(archive, currentDir, entryFile);
                if (File.Exists(Path.Combine(currentDir, "yvira.json")))
                {
                    AddFileToArchive(archive, currentDir, "yvira.json");
                }

                // Inclui pastas de recursos
                string[] dirsToInclude = { "src", "views", "assets", "dados" };
                foreach (var dirName in dirsToInclude)
                {
                    string fullDir = Path.Combine(currentDir, dirName);
                    if (Directory.Exists(fullDir))
                    {
                        foreach (string file in Directory.GetFiles(fullDir, "*.*", SearchOption.AllDirectories))
                        {
                            string relPath = Path.GetRelativePath(currentDir, file);
                            archive.CreateEntryFromFile(file, relPath);
                        }
                    }
                }
            }

            byte[] zipBytes = zipStream.ToArray();
            Console.WriteLine($"[2/3] Payload compactado: {zipBytes.Length / 1024} KB.");

            Console.WriteLine("[3/3] Selando runtime Yvira com payload no executavel unico...");
            byte[] baseExeBytes = File.ReadAllBytes(currentExe);

            using (var fs = new FileStream(targetExe, FileMode.Create, FileAccess.Write))
            {
                // Escreve o executável base do motor Yvira
                fs.Write(baseExeBytes, 0, baseExeBytes.Length);

                // Escreve o payload zip
                fs.Write(zipBytes, 0, zipBytes.Length);

                // Escreve o tamanho do payload em 8 bytes (long)
                long payloadSize = zipBytes.Length;
                byte[] sizeBytes = BitConverter.GetBytes(payloadSize);
                fs.Write(sizeBytes, 0, sizeBytes.Length);

                // Escreve a assinatura mágica no final
                fs.Write(BundleMagic, 0, BundleMagic.Length);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[SUCESSO] Executavel Selado Single-File gerado com sucesso!");
            Console.ResetColor();
            Console.WriteLine($"  Arquivo Unico: {targetExe} ({(new FileInfo(targetExe).Length) / (1024 * 1024)} MB)");
            Console.WriteLine("  Tudo (Compilador + VM + WebView2 + HTML/CSS/YV) esta embutido dentro de 1 unico arquivo!");
            Console.WriteLine("  Nao precisa de nenhuma pasta ao redor! Envie apenas esse .exe para o usuario.\n");
            return 0;
        }

        /// <summary>
        /// Detecta se o executável em execução é um Bundle Selado e extrai o app em cache se necessário
        /// </summary>
        public static string? CheckAndExtractSealedBundle()
        {
            try
            {
                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return null;

                using var fs = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length < BundleMagic.Length + 8) return null;

                // Lê os últimos bytes para checar a assinatura mágica
                fs.Seek(-BundleMagic.Length, SeekOrigin.End);
                byte[] magicBuffer = new byte[BundleMagic.Length];
                fs.Read(magicBuffer, 0, magicBuffer.Length);

                for (int i = 0; i < BundleMagic.Length; i++)
                {
                    if (magicBuffer[i] != BundleMagic[i]) return null; // Não é bundle selado
                }

                // Lê o tamanho do payload logo antes da assinatura
                fs.Seek(-(BundleMagic.Length + 8), SeekOrigin.End);
                byte[] sizeBuffer = new byte[8];
                fs.Read(sizeBuffer, 0, 8);
                long payloadLength = BitConverter.ToInt64(sizeBuffer, 0);

                if (payloadLength <= 0 || payloadLength > fs.Length) return null;

                // Posiciona no início do payload zip
                fs.Seek(-(BundleMagic.Length + 8 + payloadLength), SeekOrigin.End);
                byte[] zipBytes = new byte[payloadLength];
                fs.Read(zipBytes, 0, (int)payloadLength);

                // Extrai na pasta AppData/Local/Yvira/AppsCache/<NomeDoExe>
                string exeName = Path.GetFileNameWithoutExtension(exePath);
                string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yvira", "AppsCache", exeName);

                if (!Directory.Exists(cacheDir) || (DateTime.Now - Directory.GetLastWriteTime(cacheDir)).TotalDays > 1)
                {
                    Directory.CreateDirectory(cacheDir);
                    using var ms = new MemoryStream(zipBytes);
                    using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
                    archive.ExtractToDirectory(cacheDir, true);
                }

                // Procura o ponto de entrada
                var manifest = ProjectManifest.LoadFromCurrentDirectory(cacheDir);
                string entry = manifest?.Entry ?? "main.yv";
                string fullEntry = Path.Combine(cacheDir, entry);

                if (File.Exists(fullEntry))
                {
                    Directory.SetCurrentDirectory(cacheDir);
                    return fullEntry;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bundle Extractor]: {ex.Message}");
            }

            return null;
        }

        private static void AddFileToArchive(ZipArchive archive, string rootDir, string relPath)
        {
            string fullPath = Path.Combine(rootDir, relPath);
            if (File.Exists(fullPath))
            {
                archive.CreateEntryFromFile(fullPath, relPath);
            }
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            }
            foreach (var sub in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(sub, Path.Combine(targetDir, Path.GetFileName(sub)));
            }
        }
    }
}
