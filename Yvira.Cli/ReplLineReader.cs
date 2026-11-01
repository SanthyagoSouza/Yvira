using System;
using System.Collections.Generic;
using System.Text;

namespace Yvira.Cli;

public static class ReplLineReader
{
    private static readonly List<string> History = new();

    public static string? ReadLineWithHighlight(string prompt, bool isContinuation = false)
    {
        // Imprime o prompt com cor
        if (!isContinuation)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(prompt);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(prompt);
            Console.ResetColor();
        }

        var buffer = new StringBuilder();
        int cursor = 0;
        int historyIndex = History.Count;

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            // Enter
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                string result = buffer.ToString();
                if (!string.IsNullOrWhiteSpace(result) && (History.Count == 0 || History[^1] != result))
                {
                    History.Add(result);
                }
                return result;
            }

            // Ctrl+C ou Ctrl+D
            if (keyInfo.Key == ConsoleKey.C && (keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
            {
                Console.WriteLine();
                return null;
            }

            // Backspace
            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (cursor > 0)
                {
                    buffer.Remove(cursor - 1, 1);
                    cursor--;
                    Redraw(prompt, buffer.ToString(), cursor, isContinuation);
                }
                continue;
            }

            // Delete
            if (keyInfo.Key == ConsoleKey.Delete)
            {
                if (cursor < buffer.Length)
                {
                    buffer.Remove(cursor, 1);
                    Redraw(prompt, buffer.ToString(), cursor, isContinuation);
                }
                continue;
            }

            // Seta Esquerda
            if (keyInfo.Key == ConsoleKey.LeftArrow)
            {
                if (cursor > 0)
                {
                    cursor--;
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                }
                continue;
            }

            // Seta Direita
            if (keyInfo.Key == ConsoleKey.RightArrow)
            {
                if (cursor < buffer.Length)
                {
                    cursor++;
                    Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                }
                continue;
            }

            // Home
            if (keyInfo.Key == ConsoleKey.Home)
            {
                int diff = cursor;
                cursor = 0;
                Console.SetCursorPosition(Console.CursorLeft - diff, Console.CursorTop);
                continue;
            }

            // End
            if (keyInfo.Key == ConsoleKey.End)
            {
                int diff = buffer.Length - cursor;
                cursor = buffer.Length;
                Console.SetCursorPosition(Console.CursorLeft + diff, Console.CursorTop);
                continue;
            }

            // Seta Cima (Histórico)
            if (keyInfo.Key == ConsoleKey.UpArrow)
            {
                if (History.Count > 0 && historyIndex > 0)
                {
                    historyIndex--;
                    buffer.Clear();
                    buffer.Append(History[historyIndex]);
                    cursor = buffer.Length;
                    Redraw(prompt, buffer.ToString(), cursor, isContinuation);
                }
                continue;
            }

            // Seta Baixo (Histórico)
            if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                if (historyIndex < History.Count - 1)
                {
                    historyIndex++;
                    buffer.Clear();
                    buffer.Append(History[historyIndex]);
                    cursor = buffer.Length;
                    Redraw(prompt, buffer.ToString(), cursor, isContinuation);
                }
                else if (historyIndex == History.Count - 1)
                {
                    historyIndex = History.Count;
                    buffer.Clear();
                    cursor = 0;
                    Redraw(prompt, "", 0, isContinuation);
                }
                continue;
            }

            // Caracteres imprimíveis normais
            if (!char.IsControl(keyInfo.KeyChar))
            {
                buffer.Insert(cursor, keyInfo.KeyChar);
                cursor++;
                Redraw(prompt, buffer.ToString(), cursor, isContinuation);
            }
        }
    }

    private static void Redraw(string prompt, string text, int cursor, bool isContinuation)
    {
        // Move o cursor para o início da linha atual
        Console.CursorLeft = 0;

        // Limpa a linha inteira no console
        Console.Write(new string(' ', Math.Max(Console.BufferWidth - 1, prompt.Length + text.Length + 5)));
        Console.CursorLeft = 0;

        // Imprime prompt
        if (!isContinuation)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(prompt);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(prompt);
            Console.ResetColor();
        }

        // Imprime o texto formatado com destaque de sintaxe ao vivo
        ReplHighlighter.PrintHighlighted(text);

        // Reposiciona o cursor na posição de edição correta
        Console.CursorLeft = prompt.Length + cursor;
    }
}