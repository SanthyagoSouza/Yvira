using System;
using System.Collections.Generic;
using System.Text;

namespace Yvira.Cli;

public class CodeFormatter
{
    public static string Format(string sourceCode, int indentSpaces = 4)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return string.Empty;

        // 1. Quebra declarações múltiplas na mesma linha (ex: "let a = 10; let b = 20;")
        // preservando strings e comentários
        string expandedStatements = SeparateStatements(sourceCode);

        // 2. Normaliza quebras de linha
        string normalized = expandedStatements.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] rawLines = normalized.Split('\n');

        var formattedLines = new List<string>();
        int indentLevel = 0;
        string indentStr = new string(' ', indentSpaces);

        foreach (var rawLine in rawLines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrEmpty(line))
            {
                if (formattedLines.Count > 0 && !string.IsNullOrEmpty(formattedLines[^1]))
                {
                    formattedLines.Add(string.Empty);
                }
                continue;
            }

            // Preserva comentários
            if (line.StartsWith("#") && !line.StartsWith("#-"))
            {
                formattedLines.Add(GetIndent(indentLevel, indentStr) + line);
                continue;
            }

            // Conta fechamentos no início da linha para ajustar indentação da linha atual
            int leadingClosers = CountLeadingClosers(line);
            int effectiveIndent = Math.Max(0, indentLevel - leadingClosers);

            string formattedLineText = FormatLineTokens(line);

            formattedLines.Add(GetIndent(effectiveIndent, indentStr) + formattedLineText);

            // Ajusta o nível de indentação para a linha seguinte
            int delta = CalculateNestingDelta(line);
            indentLevel = Math.Max(0, indentLevel + delta);
        }

        return string.Join(System.Environment.NewLine, formattedLines) + System.Environment.NewLine;
    }

    private static string SeparateStatements(string code)
    {
        var sb = new StringBuilder();
        bool inString = false;
        bool inInterp = false;

        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];

            if (c == '\\' && i + 1 < code.Length && (inString || inInterp))
            {
                sb.Append(c);
                sb.Append(code[++i]);
                continue;
            }

            if (!inString && !inInterp)
            {
                if (c == '#')
                {
                    // Comentário: copia até a quebra de linha
                    while (i < code.Length && code[i] != '\n')
                    {
                        sb.Append(code[i++]);
                    }
                    if (i < code.Length) sb.Append(code[i]);
                    continue;
                }

                if (c == '$' && i + 1 < code.Length && code[i + 1] == '"')
                {
                    inInterp = true;
                    sb.Append(c);
                    sb.Append(code[++i]);
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    sb.Append(c);
                    continue;
                }

                sb.Append(c);

                // Quebra após ';' ou '{' caso venham outras instruções na mesma linha
                if (c == ';' || c == '{')
                {
                    // Olha à frente se há algo além de espaços antes do fim da linha
                    int next = i + 1;
                    while (next < code.Length && (code[next] == ' ' || code[next] == '\t'))
                        next++;

                    if (next < code.Length && code[next] != '\r' && code[next] != '\n' && code[next] != '#')
                    {
                        sb.Append('\n');
                    }
                }
            }
            else if (inString)
            {
                sb.Append(c);
                if (c == '"') inString = false;
            }
            else if (inInterp)
            {
                sb.Append(c);
                if (c == '"') inInterp = false;
            }
        }

        return sb.ToString();
    }

    private static string GetIndent(int level, string indentStr)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < level; i++) sb.Append(indentStr);
        return sb.ToString();
    }

    private static int CountLeadingClosers(string line)
    {
        int count = 0;
        foreach (char c in line)
        {
            if (c == '}' || c == ']' || c == ')')
                count++;
            else if (char.IsWhiteSpace(c))
                continue;
            else
                break;
        }
        return count;
    }

    private static int CalculateNestingDelta(string line)
    {
        int delta = 0;
        bool inString = false;
        bool inInterp = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\\' && i + 1 < line.Length && (inString || inInterp))
            {
                i++;
                continue;
            }

            if (!inString && !inInterp)
            {
                if (c == '#') break;
                if (c == '$' && i + 1 < line.Length && line[i + 1] == '"') { inInterp = true; i++; continue; }
                if (c == '"') { inString = true; continue; }

                if (c == '{' || c == '[' || c == '(') delta++;
                else if (c == '}' || c == ']' || c == ')') delta--;
            }
            else if (inString && c == '"') inString = false;
            else if (inInterp && c == '"') inInterp = false;
        }

        return delta;
    }

    private static string FormatLineTokens(string line)
    {
        var sb = new StringBuilder();
        bool inString = false;
        bool inInterp = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\\' && i + 1 < line.Length && (inString || inInterp))
            {
                sb.Append(c);
                sb.Append(line[++i]);
                continue;
            }

            if (!inString && !inInterp)
            {
                if (c == '#')
                {
                    if (sb.Length > 0 && sb[^1] != ' ') sb.Append(' ');
                    sb.Append(line[i..]);
                    break;
                }

                if (c == '$' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    inInterp = true;
                    sb.Append('$');
                    sb.Append('"');
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    sb.Append(c);
                    continue;
                }

                // Espaço antes de chaves de abertura '{'
                if (c == '{')
                {
                    if (sb.Length > 0 && sb[^1] != ' ' && sb[^1] != '(' && sb[^1] != '[')
                        sb.Append(' ');
                    sb.Append('{');
                    continue;
                }

                // Normalização de vírgula e ponto-e-vírgula
                if (c == ',' || c == ';')
                {
                    while (sb.Length > 0 && sb[^1] == ' ') sb.Length--;
                    sb.Append(c);
                    if (i + 1 < line.Length && line[i + 1] != ' ' && line[i + 1] != ')' && line[i + 1] != ']' && line[i + 1] != '}')
                    {
                        sb.Append(' ');
                    }
                    continue;
                }

                // Dois pontos ':' em dicionários
                if (c == ':')
                {
                    while (sb.Length > 0 && sb[^1] == ' ') sb.Length--;
                    sb.Append(':');
                    if (i + 1 < line.Length && line[i + 1] != ' ')
                        sb.Append(' ');
                    continue;
                }

                // Operador Flecha '=>'
                if (c == '=' && i + 1 < line.Length && line[i + 1] == '>')
                {
                    if (sb.Length > 0 && sb[^1] != ' ') sb.Append(' ');
                    sb.Append("=>");
                    i++;
                    if (i + 1 < line.Length && line[i + 1] != ' ') sb.Append(' ');
                    continue;
                }

                // Operadores binários de 2 caracteres: ==, !=, <=, >=, +=, -=, *=, /=, **
                if (i + 1 < line.Length &&
                    ((c == '=' && line[i + 1] == '=') ||
                     (c == '!' && line[i + 1] == '=') ||
                     (c == '<' && line[i + 1] == '=') ||
                     (c == '>' && line[i + 1] == '=') ||
                     (c == '+' && line[i + 1] == '=') ||
                     (c == '-' && line[i + 1] == '=') ||
                     (c == '*' && line[i + 1] == '=') ||
                     (c == '/' && line[i + 1] == '=') ||
                     (c == '*' && line[i + 1] == '*')))
                {
                    if (sb.Length > 0 && sb[^1] != ' ') sb.Append(' ');
                    sb.Append(c);
                    sb.Append(line[i + 1]);
                    i++;
                    if (i + 1 < line.Length && line[i + 1] != ' ') sb.Append(' ');
                    continue;
                }

                // Atribuição '=' ou Comparação '<' / '>' simples
                if ((c == '=' && (i + 1 >= line.Length || (line[i + 1] != '=' && line[i + 1] != '>'))) ||
                    (c == '<' && (i + 1 >= line.Length || (line[i + 1] != '=' && line[i + 1] != '<'))) ||
                    (c == '>' && (i + 1 >= line.Length || (line[i + 1] != '=' && line[i + 1] != '>'))))
                {
                    if (sb.Length > 0 && sb[^1] != ' ') sb.Append(' ');
                    sb.Append(c);
                    if (i + 1 < line.Length && line[i + 1] != ' ') sb.Append(' ');
                    continue;
                }

                // Operadores Aritméticos Simples: '+', '-', '*', '/' (evita quebrar números negativos no início ou após '(')
                if (c == '+' || c == '-' || c == '*' || c == '/')
                {
                    bool isUnary = (c == '-' || c == '+') && (sb.Length == 0 || sb[^1] == '(' || sb[^1] == '[' || sb[^1] == ',' || sb[^1] == '=');
                    if (!isUnary)
                    {
                        if (sb.Length > 0 && sb[^1] != ' ') sb.Append(' ');
                        sb.Append(c);
                        if (i + 1 < line.Length && line[i + 1] != ' ') sb.Append(' ');
                        continue;
                    }
                }

                sb.Append(c);
            }
            else if (inString)
            {
                sb.Append(c);
                if (c == '"') inString = false;
            }
            else if (inInterp)
            {
                sb.Append(c);
                if (c == '"') inInterp = false;
            }
        }

        string result = sb.ToString().TrimEnd();

        // Se a linha for apenas "}else{" ou "} else {", padroniza para "} else {"
        result = result.Replace("}else", "} else")
                       .Replace("}elif", "} elif");

        return result;
    }
}
