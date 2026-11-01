using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Yvira.Vm;

public static class PackageManager
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public static string GetGlobalCacheDirectory()
    {
        string userHome = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        string cacheDir = Path.Combine(userHome, ".yvira", "cache");
        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }
        return cacheDir;
    }

    public static string GetProjectPackagesDirectory(string projectRoot)
    {
        string pkgDir = Path.Combine(projectRoot, ".yvira", "packages");
        if (!Directory.Exists(pkgDir))
        {
            Directory.CreateDirectory(pkgDir);
        }
        return pkgDir;
    }

    /// <summary>
    /// Resolve o caminho de um pacote ou URL remota
    /// </summary>
    public static string? ResolvePackagePath(string importPath, string currentDirectory)
    {
        // 1. Se for URL HTTP/HTTPS direta
        if (importPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            importPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return FetchUrlModule(importPath);
        }

        // Procura subindo até encontrar yvira.json ou a raiz
        string? projectRoot = FindProjectRoot(currentDirectory);
        if (projectRoot != null)
        {
            // 2. Verifica em .yvira/packages/<importPath>
            string localPkgDir = Path.Combine(projectRoot, ".yvira", "packages", importPath);
            string? localEntry = FindPackageEntry(localPkgDir);
            if (localEntry != null) return localEntry;

            // 3. Verifica no cache global
            string globalPkgDir = Path.Combine(GetGlobalCacheDirectory(), importPath);
            string? globalEntry = FindPackageEntry(globalPkgDir);
            if (globalEntry != null) return globalEntry;
        }

        return null;
    }

    private static string? FindPackageEntry(string dir)
    {
        if (!Directory.Exists(dir)) return null;

        // Se houver yvira.json dentro do pacote
        string manifestPath = Path.Combine(dir, "yvira.json");
        if (File.Exists(manifestPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
                if (doc.RootElement.TryGetProperty("entry", out var entryProp))
                {
                    string entryRel = entryProp.GetString() ?? "main.yv";
                    string fullEntry = Path.Combine(dir, entryRel);
                    if (File.Exists(fullEntry)) return fullEntry;
                }
            }
            catch { }
        }

        // Convenções padrão de entrada
        string[] candidates = { "index.yv", "main.yv", "lib.yv" };
        foreach (var c in candidates)
        {
            string full = Path.Combine(dir, c);
            if (File.Exists(full)) return full;
        }

        // Se houver um arquivo com o próprio nome do diretório: dir/dir.yv
        string dirName = Path.GetFileName(dir);
        string namedYv = Path.Combine(dir, dirName + ".yv");
        if (File.Exists(namedYv)) return namedYv;

        return null;
    }

    public static string? FindProjectRoot(string startDir)
    {
        string? current = Path.GetFullPath(startDir);
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "yvira.json")))
            {
                return current;
            }
            var parent = Directory.GetParent(current);
            current = parent?.FullName;
        }
        return null;
    }

    /// <summary>
    /// Baixa um arquivo .yv de uma URL HTTP/HTTPS e armazena em cache global
    /// </summary>
    public static string FetchUrlModule(string url)
    {
        string urlHash = ComputeSha256(url).Substring(0, 16);
        string fileName = Path.GetFileName(new Uri(url).AbsolutePath);
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".yv"))
        {
            fileName = "module.yv";
        }

        string cacheFile = Path.Combine(GetGlobalCacheDirectory(), "urls", $"{urlHash}_{fileName}");
        Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);

        // Se não existir no cache local, baixa
        if (!File.Exists(cacheFile))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[Yvira Package] Baixando módulo remoto: {url}...");
            Console.ResetColor();

            var resp = _httpClient.GetAsync(url).GetAwaiter().GetResult();
            resp.EnsureSuccessStatusCode();
            string code = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            File.WriteAllText(cacheFile, code, Encoding.UTF8);
        }

        return cacheFile;
    }

    /// <summary>
    /// Instala uma dependência a partir de uma spec (ex: "github:usuario/repo" ou URL)
    /// </summary>
    public static async Task InstallDependencyAsync(string projectRoot, string pkgAlias, string sourceSpec)
    {
        string globalCache = GetGlobalCacheDirectory();
        string targetGlobalDir = Path.Combine(globalCache, pkgAlias);
        string targetLocalDir = Path.Combine(GetProjectPackagesDirectory(projectRoot), pkgAlias);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[Yvira Install] Instalando '{pkgAlias}' a partir de '{sourceSpec}'...");
        Console.ResetColor();

        // Se for github:autor/repo ou github:autor/repo@branch
        if (sourceSpec.StartsWith("github:", StringComparison.OrdinalIgnoreCase))
        {
            string repoPart = sourceSpec.Substring("github:".Length);
            string branch = "main";
            if (repoPart.Contains("@"))
            {
                var parts = repoPart.Split('@');
                repoPart = parts[0];
                branch = parts[1];
            }

            // Ex: https://raw.githubusercontent.com/autor/repo/main/
            string rawBaseUrl = $"https://raw.githubusercontent.com/{repoPart}/{branch}/";
            await DownloadGitHubPackageAsync(rawBaseUrl, targetGlobalDir);
        }
        else if (sourceSpec.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 sourceSpec.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Arquivo único remoto
            string filePath = Path.Combine(targetGlobalDir, $"{pkgAlias}.yv");
            Directory.CreateDirectory(targetGlobalDir);
            var content = await _httpClient.GetStringAsync(sourceSpec);
            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        }
        else if (Directory.Exists(Path.Combine(projectRoot, sourceSpec)) || Directory.Exists(sourceSpec))
        {
            // Caminho local existente: se a origem for diferente do destino, copia
            string sourcePath = Directory.Exists(sourceSpec) ? Path.GetFullPath(sourceSpec) : Path.GetFullPath(Path.Combine(projectRoot, sourceSpec));
            if (!sourcePath.Equals(Path.GetFullPath(targetLocalDir), StringComparison.OrdinalIgnoreCase))
            {
                CopyDirectory(sourcePath, targetLocalDir);
            }
        }
        else
        {
            throw new Exception($"Formato de origem desconhecido para o pacote '{pkgAlias}': {sourceSpec}");
        }

        // Modelo Híbrido: Copia/espelha do cache global para o projeto local se existir no cache global
        if (Directory.Exists(targetGlobalDir) && !Path.GetFullPath(targetGlobalDir).Equals(Path.GetFullPath(targetLocalDir), StringComparison.OrdinalIgnoreCase))
        {
            CopyDirectory(targetGlobalDir, targetLocalDir);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[Yvira Install] Pacote '{pkgAlias}' instalado com sucesso em .yvira/packages/{pkgAlias}!");
        Console.ResetColor();
    }

    private static async Task DownloadGitHubPackageAsync(string rawBaseUrl, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        // Tenta baixar yvira.json
        string? manifestJson = null;
        try
        {
            manifestJson = await _httpClient.GetStringAsync(rawBaseUrl + "yvira.json");
            await File.WriteAllTextAsync(Path.Combine(targetDir, "yvira.json"), manifestJson, Encoding.UTF8);
        }
        catch { }

        // Identifica arquivo principal a baixar
        string entryFile = "main.yv";
        if (manifestJson != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(manifestJson);
                if (doc.RootElement.TryGetProperty("entry", out var ep))
                {
                    entryFile = ep.GetString() ?? "main.yv";
                }
            }
            catch { }
        }

        // Tenta baixar a entry principal
        bool downloaded = false;
        string[] candidates = { entryFile, "index.yv", "main.yv", "lib.yv" };
        foreach (var candidate in candidates)
        {
            try
            {
                var content = await _httpClient.GetStringAsync(rawBaseUrl + candidate);
                await File.WriteAllTextAsync(Path.Combine(targetDir, candidate), content, Encoding.UTF8);
                downloaded = true;
                break;
            }
            catch { }
        }

        if (!downloaded)
        {
            throw new Exception($"Não foi possível encontrar nenhum arquivo fonte (.yv) no repositório.");
        }
    }

    public static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string destSub = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSub);
        }
    }

    private static string ComputeSha256(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder();
        foreach (byte b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
