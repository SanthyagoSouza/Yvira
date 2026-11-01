using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yvira.Cli;

public class ProjectManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "meu_projeto_yvira";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "Projeto desenvolvido na linguagem Yvira";

    [JsonPropertyName("entry")]
    public string Entry { get; set; } = "main.yv";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "Yvira Developer";

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static ProjectManifest? LoadFromCurrentDirectory(string dir)
    {
        string manifestPath = Path.Combine(dir, "yvira.json");
        if (!File.Exists(manifestPath)) return null;

        try
        {
            string json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<ProjectManifest>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(string projectDir)
    {
        string manifestPath = Path.Combine(projectDir, "yvira.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(manifestPath, json);
    }

    public static void CreateNewProject(string projectDir, string projectName)
    {
        if (!Directory.Exists(projectDir))
        {
            Directory.CreateDirectory(projectDir);
        }

        // 1. Cria yvira.json
        var manifest = new ProjectManifest
        {
            Name = projectName,
            Version = "1.0.0",
            Description = $"Projeto {projectName} em Yvira",
            Entry = "main.yv",
            Dependencies = new Dictionary<string, string>
            {
                ["math_utils"] = "https://raw.githubusercontent.com/yvira-lang/examples/main/math_utils.yv"
            }
        };
        manifest.Save(projectDir);

        // 2. Cria main.yv
        string mainCode = "# Ponto de entrada principal do projeto Yvira\n" +
                          "import \"fs\" as fs\n" +
                          "import \".env\" as env\n\n" +
                          "print(\"========================================\")\n" +
                          "print(\"  Bem-vindo ao projeto:\", env.APP_NAME)\n" +
                          "print(\"  Versao:\", env.APP_VERSION)\n" +
                          "print(\"========================================\\n\")\n\n" +
                          "class Calculadora {\n" +
                          "    fn somar(a, b) {\n" +
                          "        return a + b\n" +
                          "    }\n" +
                          "}\n\n" +
                          "let calc = Calculadora()\n" +
                          "print(\"Execucao bem-sucedida! 10 + 20 =\", calc.somar(10, 20))\n";
        File.WriteAllText(Path.Combine(projectDir, "main.yv"), mainCode);

        // 3. Cria .env
        string envContent = $"APP_NAME=\"{projectName}\"\nAPP_VERSION=\"1.0.0\"\nDEBUG=true\nPORT=8080\n";
        File.WriteAllText(Path.Combine(projectDir, ".env"), envContent);

        // 4. Cria .gitignore
        string gitignore = "# Logs e saida temporaria\n*.log\n*.tmp\nbin/\nobj/\n.yvira/\n";
        File.WriteAllText(Path.Combine(projectDir, ".gitignore"), gitignore);
    }
}
