using System;
using System.Collections.Generic;
using System.IO;

namespace Yvira.Vm;

public static class FileParsers
{
    public static YviraMap ParseEnv(string content)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
            {
                continue;
            }

            int eqIdx = trimmed.IndexOf('=');
            if (eqIdx > 0)
            {
                string key = trimmed.Substring(0, eqIdx).Trim();
                string val = trimmed.Substring(eqIdx + 1).Trim();

                // Trata aspas duplas ou simples
                if (val.Length >= 2 && ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'"))))
                {
                    val = val.Substring(1, val.Length - 2);
                }

                // Tenta converter automaticamente para número ou booleano se aplicável
                if (val.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    map[key] = true;
                }
                else if (val.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    map[key] = false;
                }
                else if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double num))
                {
                    map[key] = num;
                }
                else
                {
                    map[key] = val;
                }
            }
        }
        return new YviraMap(map);
    }

    public static object? ParseJson(string content)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        return ConvertJsonElement(doc.RootElement);
    }

    private static object? ConvertJsonElement(System.Text.Json.JsonElement elem)
    {
        switch (elem.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in elem.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElement(prop.Value);
                }
                return new YviraMap(dict);

            case System.Text.Json.JsonValueKind.Array:
                var list = new YviraList();
                foreach (var item in elem.EnumerateArray())
                {
                    list.Add(ConvertJsonElement(item));
                }
                return list;

            case System.Text.Json.JsonValueKind.String:
                return elem.GetString();

            case System.Text.Json.JsonValueKind.Number:
                return elem.GetDouble();

            case System.Text.Json.JsonValueKind.True:
                return true;

            case System.Text.Json.JsonValueKind.False:
                return false;

            case System.Text.Json.JsonValueKind.Null:
            default:
                return null;
        }
    }
}