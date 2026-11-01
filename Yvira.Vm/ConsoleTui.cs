using System;
using System.Collections.Generic;
using System.Text;

namespace Yvira.Vm
{
    /// <summary>
    /// Motor TUI para renderização de menus interativos, caixas, tabelas e elementos visuais no terminal.
    /// </summary>
    public static class ConsoleTui
    {
        public static void Clear()
        {
            try
            {
                Console.Clear();
            }
            catch { }
        }

        public static void SetCursorPosition(int left, int top)
        {
            try
            {
                Console.SetCursorPosition(Math.Max(0, left), Math.Max(0, top));
            }
            catch { }
        }

        public static (int left, int top) GetCursorPosition()
        {
            try
            {
                return (Console.CursorLeft, Console.CursorTop);
            }
            catch
            {
                return (0, 0);
            }
        }

        /// <summary>
        /// Menu interativo navegável por setas (Cima / Baixo) e Enter.
        /// Retorna o índice escolhido (0 a N-1) ou -1 se cancelado com Esc / Ctrl+C.
        /// </summary>
        public static int ShowInteractiveMenu(string title, IReadOnlyList<string> options, int defaultIndex = 0)
        {
            if (options == null || options.Count == 0) return -1;

            int selected = Math.Clamp(defaultIndex, 0, options.Count - 1);
            bool cursorVisibleOriginal = true;
            try
            {
                cursorVisibleOriginal = Console.CursorVisible;
                Console.CursorVisible = false;
            }
            catch { }

            int startTop = 0;
            try { startTop = Console.CursorTop; } catch { }

            // Renderiza o cabeçalho do menu se houver título
            if (!string.IsNullOrWhiteSpace(title))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n  === {title} ===");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  (Use as setas ↑ / ↓ para navegar, Enter para confirmar, Esc para cancelar)\n");
                Console.ResetColor();
                try { startTop = Console.CursorTop; } catch { }
            }

            void RenderOptions()
            {
                try { Console.SetCursorPosition(0, startTop); } catch { }

                for (int i = 0; i < options.Count; i++)
                {
                    if (i == selected)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Cyan;
                        Console.Write("  ▶ ");
                        Console.Write(options[i].PadRight(40));
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("    ");
                        Console.Write(options[i].PadRight(40));
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                }
            }

            RenderOptions();

            while (true)
            {
                ConsoleKeyInfo key;
                try
                {
                    key = Console.ReadKey(true);
                }
                catch
                {
                    break;
                }

                if (key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.K)
                {
                    selected = (selected - 1 + options.Count) % options.Count;
                    RenderOptions();
                }
                else if (key.Key == ConsoleKey.DownArrow || key.Key == ConsoleKey.J)
                {
                    selected = (selected + 1) % options.Count;
                    RenderOptions();
                }
                else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar)
                {
                    try
                    {
                        Console.CursorVisible = cursorVisibleOriginal;
                        Console.WriteLine();
                    }
                    catch { }
                    return selected;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    try
                    {
                        Console.CursorVisible = cursorVisibleOriginal;
                        Console.WriteLine();
                    }
                    catch { }
                    return -1;
                }
            }

            try { Console.CursorVisible = cursorVisibleOriginal; } catch { }
            return selected;
        }

        /// <summary>
        /// Confirmação interativa (s/n, Sim/Não). Retorna true para sim e false para não.
        /// </summary>
        public static bool Confirm(string prompt, bool defaultYes = true)
        {
            string hint = defaultYes ? "[S/n]" : "[s/N]";
            Console.Write($"{prompt} {hint} ");
            
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine(defaultYes ? "Sim" : "Não");
                    return defaultYes;
                }
                if (key.Key == ConsoleKey.S || key.KeyChar == 's' || key.KeyChar == 'S' || key.Key == ConsoleKey.Y || key.KeyChar == 'y')
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Sim");
                    Console.ResetColor();
                    return true;
                }
                if (key.Key == ConsoleKey.N || key.KeyChar == 'n' || key.KeyChar == 'N')
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Não");
                    Console.ResetColor();
                    return false;
                }
            }
        }

        /// <summary>
        /// Desenha uma caixa decorada em volta do texto.
        /// </summary>
        public static void DrawBox(string title, string content, ConsoleColor borderColor = ConsoleColor.Cyan)
        {
            string normalized = content.Replace("\\n", "\n");
            var lines = normalized.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int maxLen = title.Length;
            foreach (var l in lines)
            {
                if (l.Length > maxLen) maxLen = l.Length;
            }
            maxLen = Math.Max(maxLen, 30);
            int width = maxLen + 4;

            Console.ForegroundColor = borderColor;
            Console.Write("┌");
            if (!string.IsNullOrEmpty(title))
            {
                Console.Write($" {title} ");
                Console.Write(new string('─', width - title.Length - 4));
            }
            else
            {
                Console.Write(new string('─', width - 2));
            }
            Console.WriteLine("┐");

            foreach (var l in lines)
            {
                Console.Write("│ ");
                Console.ResetColor();
                Console.Write(l.PadRight(width - 4));
                Console.ForegroundColor = borderColor;
                Console.WriteLine(" │");
            }

            Console.Write("└");
            Console.Write(new string('─', width - 2));
            Console.WriteLine("┘");
            Console.ResetColor();
        }

        /// <summary>
        /// Desenha uma barra de progresso no terminal com percentual.
        /// </summary>
        public static void DrawProgressBar(double progress, int total = 100, string label = "", int barWidth = 30)
        {
            double ratio = Math.Clamp(progress / (double)total, 0.0, 1.0);
            int filled = (int)Math.Round(ratio * barWidth);
            int empty = barWidth - filled;

            int pct = (int)Math.Round(ratio * 100.0);

            Console.Write("\r");
            if (!string.IsNullOrEmpty(label))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{label} ");
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(new string('█', filled));
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(new string('░', empty));
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{pct,3}% ");
            Console.ResetColor();

            if (pct >= 100)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Renderiza uma tabela estilizada no terminal a partir de headers e linhas.
        /// </summary>
        public static void DrawTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            if (headers == null || headers.Count == 0) return;

            int cols = headers.Count;
            int[] colWidths = new int[cols];

            for (int c = 0; c < cols; c++)
            {
                colWidths[c] = headers[c].Length;
            }

            if (rows != null)
            {
                foreach (var r in rows)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        string val = c < r.Count ? (r[c] ?? "") : "";
                        if (val.Length > colWidths[c]) colWidths[c] = val.Length;
                    }
                }
            }

            for (int c = 0; c < cols; c++)
            {
                colWidths[c] = Math.Max(colWidths[c], 3) + 2; // padding
            }

            // Topo: ┌───┬───┐
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("┌");
            for (int c = 0; c < cols; c++)
            {
                Console.Write(new string('─', colWidths[c]));
                if (c < cols - 1) Console.Write("┬");
            }
            Console.WriteLine("┐");

            // Headers
            Console.Write("│");
            for (int c = 0; c < cols; c++)
            {
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($" {headers[c].PadRight(colWidths[c] - 1)}");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("│");
            }
            Console.WriteLine();

            // Divisor: ├───┼───┤
            Console.Write("├");
            for (int c = 0; c < cols; c++)
            {
                Console.Write(new string('─', colWidths[c]));
                if (c < cols - 1) Console.Write("┼");
            }
            Console.WriteLine("┤");

            // Linhas
            if (rows != null)
            {
                foreach (var r in rows)
                {
                    Console.Write("│");
                    for (int c = 0; c < cols; c++)
                    {
                        string val = c < r.Count ? (r[c] ?? "") : "";
                        Console.ResetColor();
                        Console.Write($" {val.PadRight(colWidths[c] - 1)}");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write("│");
                    }
                    Console.WriteLine();
                }
            }

            // Base: └───┴───┘
            Console.Write("└");
            for (int c = 0; c < cols; c++)
            {
                Console.Write(new string('─', colWidths[c]));
                if (c < cols - 1) Console.Write("┴");
            }
            Console.WriteLine("┘");
            Console.ResetColor();
        }
    }
}
