using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Yvira.Cli;

public static class Updater
{
    public const string CurrentVersion = "2.1.0";
    private const string VersionEndpoint = "https://raw.githubusercontent.com/yvira-lang/yvira/main/version.json";

    public static async Task<int> RunUpgradeAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Verificando atualizacoes do Yvira...");
        Console.ResetColor();
        Console.WriteLine($"  Versao atual: v{CurrentVersion}");

        string? currentExePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExePath) || !File.Exists(currentExePath))
        {
            currentExePath = Path.Combine(AppContext.BaseDirectory, "yvira.exe");
        }

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.Add("User-Agent", "Yvira-Cli-Updater");

            string responseJson = await client.GetStringAsync(VersionEndpoint);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            string latestVersion = root.GetProperty("version").GetString() ?? CurrentVersion;
            string downloadUrl = root.GetProperty("windows_x64_url").GetString() ?? "";

            if (latestVersion == CurrentVersion)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[OK] Voce ja esta utilizando a versao mais recente (v{CurrentVersion})!");
                Console.ResetColor();
                return 0;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nNova versao disponivel: v{latestVersion}!");
            Console.ResetColor();

            if (string.IsNullOrEmpty(downloadUrl))
            {
                Console.WriteLine("URL de download nao disponivel no momento.");
                return 1;
            }

            Console.WriteLine("Baixando nova versao...");
            byte[] binaryBytes = await client.GetByteArrayAsync(downloadUrl);

            string tempFile = currentExePath + ".update";
            await File.WriteAllBytesAsync(tempFile, binaryBytes);

            ApplyUpdate(currentExePath, tempFile);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[Sucesso] Yvira atualizado para v{latestVersion} com sucesso!");
            Console.ResetColor();
            return 0;
        }
        catch (HttpRequestException)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[Aviso] Nao foi possivel conectar ao servidor de atualizacoes online.");
            Console.ResetColor();
            Console.WriteLine("Caso esteja desenvolvendo localmente, use o script de publicacao local:");
            Console.WriteLine("  powershell -ExecutionPolicy Bypass -File scripts\\publish_local.ps1\n");
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Erro] Falha ao atualizar: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private static void ApplyUpdate(string currentExe, string newExe)
    {
        string oldExe = currentExe + ".old";
        if (File.Exists(oldExe))
        {
            try { File.Delete(oldExe); } catch { }
        }

        // No Windows, podemos renomear um arquivo em execucao (.exe)
        // e mover o novo para o lugar original!
        File.Move(currentExe, oldExe);
        File.Move(newExe, currentExe);
    }
}
