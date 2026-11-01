using System;
using System.Collections.Generic;
using System.Text;

namespace Yvira.Cli;

public static class ReplHighlighter
{
    private static readonly HashSet<string> ControlKeywords = new(StringComparer.Ordinal)
    {
        "if", "else", "while", "for", "foreach", "in", "break", "continue",
        "return", "try", "catch", "finally", "throw"
    };

    private static readonly HashSet<string> DeclKeywords = new(StringComparer.Ordinal)
    {
        "fn", "class", "let", "const", "import", "as"
    };

    private static readonly HashSet<string> SpecialValues = new(StringComparer.Ordinal)
    {
        "this", "true", "false", "null", "and", "or", "not"
    };

    public static void PrintHighlighted(string line)
    {
        int i = 0;
        int len = line.Length;

        while (i < len)
        {
            char ch = line[i];

            // 1. Comentários com #
            if (ch == '#')
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(line.Substring(i));
                Console.ResetColor();
                return;
            }

            // 2. Strings ("..." ou '...')
            if (ch == '"' || ch == '\'')
            {
                char quote = ch;
                int start = i;
                i++;
                while (i < len && line[i] != quote)
                {
                    if (line[i] == '\\' && i + 1 < len) i++;
                    i++;
                }
                if (i < len && line[i] == quote) i++;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(line.Substring(start, i - start));
                Console.ResetColor();
                continue;
            }

            // 3. Números
            if (char.IsDigit(ch) || (ch == '.' && i + 1 < len && char.IsDigit(line[i + 1])))
            {
                int start = i;
                while (i < len && (char.IsDigit(line[i]) || line[i] == '.'))
                {
                    i++;
                }
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write(line.Substring(start, i - start));
                Console.ResetColor();
                continue;
            }

            // 4. Identificadores e Palavras-chave
            if (char.IsLetter(ch) || ch == '_')
            {
                int start = i;
                while (i < len && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                {
                    i++;
                }
                string word = line.Substring(start, i - start);

                if (ControlKeywords.Contains(word))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                }
                else if (DeclKeywords.Contains(word))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                else if (SpecialValues.Contains(word))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }

                Console.Write(word);
                Console.ResetColor();
                continue;
            }

            // 5. Operadores e Delimitadores
            if ("{}()[]+-*/%=><!&|,;.".IndexOf(ch) >= 0)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(ch);
                Console.ResetColor();
                i++;
                continue;
            }

            // 6. Espaços e outros
            Console.Write(ch);
            i++;
        }
    }
}