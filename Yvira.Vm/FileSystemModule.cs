using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Yvira.Vm;

public static class FileSystemModule
{
    public static YviraMap CreateModule(string baseDirectory)
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // fs.read(path) -> string
        methods["read"] = new HostFunction("fs.read", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("fs.read requer o caminho do arquivo.");
            string path = ResolvePath(baseDirectory, args[0]!.ToString()!);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Arquivo não encontrado: {path}");
            return File.ReadAllText(path);
        });

        // fs.readLines(path) -> YviraList
        methods["readLines"] = new HostFunction("fs.readLines", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("fs.readLines requer o caminho do arquivo.");
            string path = ResolvePath(baseDirectory, args[0]!.ToString()!);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Arquivo não encontrado: {path}");
            var lines = File.ReadAllLines(path);
            return new YviraList(lines.Select(l => (object?)l));
        });

        // fs.write(path, content) -> bool
        methods["write"] = new HostFunction("fs.write", (interp, args) =>
        {
            if (args.Count < 2)
                throw new Exception("fs.write requer caminho e conteúdo.");
            string path = ResolvePath(baseDirectory, args[0]?.ToString() ?? "");
            string content = args[1]?.ToString() ?? "";
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, content);
            return true;
        });

        // fs.append(path, content) -> bool
        methods["append"] = new HostFunction("fs.append", (interp, args) =>
        {
            if (args.Count < 2)
                throw new Exception("fs.append requer caminho e conteúdo.");
            string path = ResolvePath(baseDirectory, args[0]?.ToString() ?? "");
            string content = args[1]?.ToString() ?? "";
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.AppendAllText(path, content);
            return true;
        });

        // fs.exists(path) -> bool
        methods["exists"] = new HostFunction("fs.exists", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) return false;
            string path = ResolvePath(baseDirectory, args[0]!.ToString()!);
            return File.Exists(path) || Directory.Exists(path);
        });

        // fs.remove(path) -> bool
        methods["remove"] = new HostFunction("fs.remove", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) return false;
            string path = ResolvePath(baseDirectory, args[0]!.ToString()!);
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                return true;
            }
            return false;
        });

        // fs.list(dir) -> YviraList
        methods["list"] = new HostFunction("fs.list", (interp, args) =>
        {
            string dir = args.Count > 0 && args[0] != null ? ResolvePath(baseDirectory, args[0]!.ToString()!) : baseDirectory;
            if (!Directory.Exists(dir))
                throw new DirectoryNotFoundException($"Diretório não encontrado: {dir}");
            var items = Directory.GetFileSystemEntries(dir).Select(p => (object?)Path.GetFileName(p));
            return new YviraList(items);
        });

        // fs.readCsv(path) -> YviraMatrix
        methods["readCsv"] = new HostFunction("fs.readCsv", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("fs.readCsv requer o caminho do arquivo .csv.");
            string path = ResolvePath(baseDirectory, args[0]!.ToString()!);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Arquivo CSV não encontrado: {path}");

            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count == 0) return YviraMatrix.Zeros(0, 0);

            var rowData = new List<double[]>();
            int expectedCols = -1;

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (expectedCols == -1) expectedCols = parts.Length;
                var row = new double[parts.Length];
                for (int c = 0; c < parts.Length; c++)
                {
                    double.TryParse(parts[c].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out row[c]);
                }
                rowData.Add(row);
            }

            int rCount = rowData.Count;
            int cCount = expectedCols;
            var data = new double[rCount, cCount];
            for (int r = 0; r < rCount; r++)
            {
                for (int c = 0; c < Math.Min(cCount, rowData[r].Length); c++)
                {
                    data[r, c] = rowData[r][c];
                }
            }
            return new YviraMatrix(data);
        });

        // fs.writeCsv(path, matrix) -> bool
        methods["writeCsv"] = new HostFunction("fs.writeCsv", (interp, args) =>
        {
            if (args.Count < 2 || !(args[1] is YviraMatrix mat))
                throw new Exception("fs.writeCsv requer o caminho e um objeto do tipo Matrix.");
            string path = ResolvePath(baseDirectory, args[0]?.ToString() ?? "");

            using var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8);
            for (int r = 0; r < mat.Rows; r++)
            {
                var rowVals = new List<string>();
                for (int c = 0; c < mat.Cols; c++)
                {
                    rowVals.Add(mat.Get(r, c).ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                writer.WriteLine(string.Join(",", rowVals));
            }
            return true;
        });

        return new YviraMap(methods);
    }

    private static string ResolvePath(string baseDirectory, string path)
    {
        if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }
}

public class HostFunction
{
    public string Name { get; }
    public Func<Interpreter, IReadOnlyList<object?>, object?> Callback { get; }

    public HostFunction(string name, Func<Interpreter, IReadOnlyList<object?>, object?> callback)
    {
        Name = name;
        Callback = callback;
    }

    public object? Call(Interpreter interp, IReadOnlyList<object?> args) => Callback(interp, args);

    public override string ToString() => $"<função nativa {Name}>";
}