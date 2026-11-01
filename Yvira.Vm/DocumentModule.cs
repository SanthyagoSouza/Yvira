using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Yvira.Vm;

public class PdfBuilderContext
{
    public string Title { get; set; } = "Documento Yvira";
    public string? TitleColor { get; set; } = "#1e3a8a";
    public string? TitleAlign { get; set; } = "left";
    public string? Subtitle { get; set; }
    public string? Footer { get; set; } = "Yvira Language Platform • Document Engine";
    public string PageSize { get; set; } = "A4";
    public string Orientation { get; set; } = "portrait";
    public List<PdfSection> Sections { get; } = new();

    public abstract class PdfSection { }

    public class TextSection : PdfSection
    {
        public string Text { get; set; } = "";
        public float FontSize { get; set; } = 11;
        public string? Color { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public string Align { get; set; } = "left";
    }

    public class TableSection : PdfSection
    {
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
        public string Theme { get; set; } = "striped"; // striped, dark, minimal
        public string? HeaderColor { get; set; }
    }

    public class ViewSection : PdfSection
    {
        public string HtmlContent { get; set; } = "";
    }
}

public static class DocumentModule
{
    static DocumentModule()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.EnableDebugging = false;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        QuestPDF.Settings.UseSystemFonts = true;
    }

    public static YviraMap CreateModule(string baseDirectory)
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // ==========================================
        // 1. GERAÇÃO DE PLANILHAS EXCEL (XLSX)
        // ==========================================
        methods["writeExcel"] = new HostFunction("doc.writeExcel", (interp, args) =>
        {
            if (args.Count < 2 || args[0] == null || args[1] == null)
                throw new Exception("doc.writeExcel requer (caminho, dados).");

            string relativePath = args[0]!.ToString()!;
            string fullPath = Path.IsPathRooted(relativePath) ? relativePath : Path.GetFullPath(Path.Combine(baseDirectory, relativePath));

            string? dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Planilha1");

            int currentRow = 1;

            if (args[1] is YviraMap map)
            {
                if (map.Get("title") is string titleText)
                {
                    ws.Cell(currentRow, 1).Value = titleText;
                    ws.Cell(currentRow, 1).Style.Font.Bold = true;
                    ws.Cell(currentRow, 1).Style.Font.FontSize = 14;
                    currentRow += 2;
                }

                if (map.Get("headers") is YviraList headers)
                {
                    for (int c = 0; c < headers.Count; c++)
                    {
                        var cell = ws.Cell(currentRow, c + 1);
                        cell.Value = headers.Get(c)?.ToString() ?? "";
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(230, 235, 245);
                        cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                    }
                    currentRow++;
                }

                if (map.Get("rows") is YviraList rows)
                {
                    for (int r = 0; r < rows.Count; r++)
                    {
                        var rowObj = rows.Get(r);
                        if (rowObj is YviraList rowList)
                        {
                            for (int c = 0; c < rowList.Count; c++)
                            {
                                SetCellValue(ws.Cell(currentRow, c + 1), rowList.Get(c));
                            }
                        }
                        else if (rowObj is IReadOnlyList<object?> rawList)
                        {
                            for (int c = 0; c < rawList.Count; c++)
                            {
                                SetCellValue(ws.Cell(currentRow, c + 1), rawList[c]);
                            }
                        }
                        currentRow++;
                    }
                }
            }
            else if (args[1] is YviraMatrix matrix)
            {
                for (int r = 0; r < matrix.Rows; r++)
                {
                    for (int c = 0; c < matrix.Cols; c++)
                    {
                        ws.Cell(r + 1, c + 1).Value = matrix.Get(r, c);
                    }
                }
            }
            else if (args[1] is YviraList rawTable)
            {
                // Ingestão Polimórfica Direta: Tabela do @query ou Lista de Maps
                ExtractTableData(rawTable, out var headers, out var rows);
                if (headers.Count > 0)
                {
                    for (int c = 0; c < headers.Count; c++)
                    {
                        var cell = ws.Cell(1, c + 1);
                        cell.Value = headers[c];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(230, 235, 245);
                        cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                    }
                    currentRow = 2;
                }

                for (int r = 0; r < rows.Count; r++)
                {
                    for (int c = 0; c < rows[r].Count; c++)
                    {
                        ws.Cell(currentRow + r, c + 1).Value = rows[r][c];
                    }
                }
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(fullPath);
            return true;
        });

        methods["readExcel"] = new HostFunction("doc.readExcel", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("doc.readExcel requer o caminho do arquivo.");

            string relativePath = args[0]!.ToString()!;
            string fullPath = Path.IsPathRooted(relativePath) ? relativePath : Path.GetFullPath(Path.Combine(baseDirectory, relativePath));

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Arquivo Excel não encontrado: {fullPath}");

            using var workbook = new XLWorkbook(fullPath);
            var ws = args.Count > 1 && args[1] != null
                ? workbook.Worksheet(args[1]!.ToString()!)
                : workbook.Worksheets.First();

            var resultRows = new List<object?>();
            foreach (var row in ws.RowsUsed())
            {
                var rowItems = new List<object?>();
                foreach (var cell in row.Cells(1, row.LastCellUsed()?.Address.ColumnNumber ?? 1))
                {
                    if (cell.TryGetValue<double>(out double dVal))
                        rowItems.Add(dVal);
                    else if (cell.TryGetValue<bool>(out bool bVal))
                        rowItems.Add(bVal);
                    else
                        rowItems.Add(cell.GetString());
                }
                resultRows.Add(new YviraList(rowItems));
            }

            return new YviraList(resultRows);
        });

        // ==========================================
        // 2. GERAÇÃO DE PDF POLIMÓRFICA & DECLARATIVA
        // ==========================================
        methods["writePdf"] = new HostFunction("doc.writePdf", (interp, args) =>
        {
            if (args.Count < 2 || args[0] == null || args[1] == null)
                throw new Exception("doc.writePdf requer (caminho, dados).");

            string relativePath = args[0]!.ToString()!;
            string fullPath = Path.IsPathRooted(relativePath) ? relativePath : Path.GetFullPath(Path.Combine(baseDirectory, relativePath));

            string? dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var ctx = new PdfBuilderContext();

            if (args[1] is YviraMap config)
            {
                ctx.Title = config.Get("title")?.ToString() ?? "Documento Yvira";
                ctx.Subtitle = config.Get("subtitle")?.ToString();
                ctx.Footer = config.Get("footer")?.ToString() ?? ctx.Footer;
                ctx.PageSize = config.Get("size")?.ToString() ?? "A4";

                string? text = config.Get("content")?.ToString() ?? config.Get("text")?.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    ctx.Sections.Add(new PdfBuilderContext.TextSection { Text = text });
                }

                // Ingestão Polimórfica da Tabela (Suporta: @query direto, List of Maps, List of Lists, Matrix)
                object? tableData = config.Get("data") ?? config.Get("table") ?? config.Get("rows");
                if (tableData != null)
                {
                    ExtractTableData(tableData, out var autoHeaders, out var autoRows);

                    // Se o usuário passou headers manuais explicitamente, usa eles
                    if (config.Get("headers") is YviraList customHeaders)
                    {
                        autoHeaders = customHeaders.Select(h => h?.ToString() ?? "").ToList();
                    }

                    if (autoRows.Count > 0)
                    {
                        ctx.Sections.Add(new PdfBuilderContext.TableSection
                        {
                            Headers = autoHeaders,
                            Rows = autoRows,
                            Theme = config.Get("theme")?.ToString() ?? "striped"
                        });
                    }
                }
            }
            else if (args[1] is YviraList rawList)
            {
                // Ingestão Direta: doc.writePdf("arquivo.pdf", @query "SELECT ...")
                ExtractTableData(rawList, out var autoHeaders, out var autoRows);
                ctx.Sections.Add(new PdfBuilderContext.TableSection
                {
                    Headers = autoHeaders,
                    Rows = autoRows
                });
            }

            RenderPdfContext(ctx, fullPath);
            return true;
        });

        // doc.fromView("template.html", "saida.pdf", dadosOpcionais)
        methods["fromView"] = new HostFunction("doc.fromView", (interp, args) =>
        {
            if (args.Count < 2)
                throw new Exception("doc.fromView requer (caminhoView, caminhoPdf).");

            string viewPath = args[0]?.ToString() ?? "";
            string pdfPath = args[1]?.ToString() ?? "";

            string fullViewPath = Path.IsPathRooted(viewPath) ? viewPath : Path.GetFullPath(Path.Combine(baseDirectory, viewPath));
            string fullPdfPath = Path.IsPathRooted(pdfPath) ? pdfPath : Path.GetFullPath(Path.Combine(baseDirectory, pdfPath));

            if (!File.Exists(fullViewPath))
                throw new FileNotFoundException($"Arquivo de visualização/view não encontrado: {fullViewPath}");

            string html = File.ReadAllText(fullViewPath);

            // Se foram passados dados para substituição de variáveis simples {{chave}}
            if (args.Count >= 3 && args[2] is YviraMap dataMap)
            {
                foreach (var entry in dataMap)
                {
                    html = html.Replace("{{" + entry.Key + "}}", entry.Value?.ToString() ?? "");
                }
            }

            var ctx = new PdfBuilderContext
            {
                Title = Path.GetFileNameWithoutExtension(fullViewPath)
            };
            ctx.Sections.Add(new PdfBuilderContext.ViewSection { HtmlContent = html });

            RenderPdfContext(ctx, fullPdfPath);
            return true;
        });

        return new YviraMap(methods);
    }

    public static void ExtractTableData(object tableSource, out List<string> headers, out List<List<string>> rows)
    {
        headers = new List<string>();
        rows = new List<List<string>>();

        if (tableSource is YviraList list)
        {
            if (list.Count == 0) return;

            // Caso 1: Resultado de @query SQL -> Lista de YviraMap
            if (list.Get(0) is YviraMap firstMap)
            {
                headers = firstMap.Keys.ToList();
                for (int r = 0; r < list.Count; r++)
                {
                    if (list.Get(r) is YviraMap map)
                    {
                        var rowVals = new List<string>();
                        foreach (var h in headers)
                        {
                            var v = map.Get(h);
                            rowVals.Add(FormatDisplayValue(v));
                        }
                        rows.Add(rowVals);
                    }
                }
                return;
            }

            // Caso 2: Lista de Listas (List of Lists)
            if (list.Get(0) is YviraList || list.Get(0) is IReadOnlyList<object?>)
            {
                for (int r = 0; r < list.Count; r++)
                {
                    var rowVals = new List<string>();
                    if (list.Get(r) is YviraList rowList)
                    {
                        for (int c = 0; c < rowList.Count; c++)
                            rowVals.Add(FormatDisplayValue(rowList.Get(c)));
                    }
                    else if (list.Get(r) is IReadOnlyList<object?> rawL)
                    {
                        for (int c = 0; c < rawL.Count; c++)
                            rowVals.Add(FormatDisplayValue(rawL[c]));
                    }
                    rows.Add(rowVals);
                }
                return;
            }

            // Caso 3: Lista simples unidimensional
            headers.Add("Valor");
            for (int r = 0; r < list.Count; r++)
            {
                rows.Add(new List<string> { FormatDisplayValue(list.Get(r)) });
            }
        }
        else if (tableSource is YviraMatrix matrix)
        {
            for (int c = 0; c < matrix.Cols; c++) headers.Add($"Col {c + 1}");
            for (int r = 0; r < matrix.Rows; r++)
            {
                var rowVals = new List<string>();
                for (int c = 0; c < matrix.Cols; c++)
                {
                    rowVals.Add(matrix.Get(r, c).ToString("0.####"));
                }
                rows.Add(rowVals);
            }
        }
    }

    private static string FormatDisplayValue(object? val)
    {
        if (val == null) return "-";
        if (val is double d) return d.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        return val.ToString() ?? "";
    }

    public static void RenderPdfContext(PdfBuilderContext ctx, string fullPath)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                var pageSize = ctx.PageSize.Equals("Letter", StringComparison.OrdinalIgnoreCase) ? PageSizes.Letter
                    : (ctx.PageSize.Equals("Legal", StringComparison.OrdinalIgnoreCase) ? PageSizes.Legal : PageSizes.A4);

                if (ctx.Orientation.Equals("landscape", StringComparison.OrdinalIgnoreCase))
                    pageSize = pageSize.Landscape();

                page.Size(pageSize);

                page.Margin(35);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                // Cabeçalho Principal
                page.Header().Column(col =>
                {
                    var titleColor = ParseColor(ctx.TitleColor, Colors.Blue.Darken2);
                    var titleBlock = col.Item().Text(ctx.Title).Bold().FontSize(20).FontColor(titleColor);
                    if (ctx.TitleAlign?.Equals("center", StringComparison.OrdinalIgnoreCase) == true) titleBlock.AlignCenter();
                    else if (ctx.TitleAlign?.Equals("right", StringComparison.OrdinalIgnoreCase) == true) titleBlock.AlignRight();

                    if (!string.IsNullOrEmpty(ctx.Subtitle))
                    {
                        var subBlock = col.Item().Text(ctx.Subtitle).FontSize(12).FontColor(Colors.Grey.Medium);
                        if (ctx.TitleAlign?.Equals("center", StringComparison.OrdinalIgnoreCase) == true) subBlock.AlignCenter();
                        else if (ctx.TitleAlign?.Equals("right", StringComparison.OrdinalIgnoreCase) == true) subBlock.AlignRight();
                    }
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                // Conteúdo
                page.Content().PaddingVertical(15).Column(col =>
                {
                    foreach (var section in ctx.Sections)
                    {
                        if (section is PdfBuilderContext.TextSection textSec)
                        {
                            var tBlock = col.Item().PaddingBottom(10).Text(textSec.Text).FontSize(textSec.FontSize);
                            if (textSec.Bold) tBlock.Bold();
                            if (textSec.Italic) tBlock.Italic();
                            if (!string.IsNullOrEmpty(textSec.Color)) tBlock.FontColor(ParseColor(textSec.Color, Colors.Black));
                            if (textSec.Align.Equals("center", StringComparison.OrdinalIgnoreCase)) tBlock.AlignCenter();
                            else if (textSec.Align.Equals("right", StringComparison.OrdinalIgnoreCase)) tBlock.AlignRight();
                        }
                        else if (section is PdfBuilderContext.TableSection tableSec)
                        {
                            int colsCount = tableSec.Headers.Count > 0
                                ? tableSec.Headers.Count
                                : (tableSec.Rows.Count > 0 ? tableSec.Rows[0].Count : 1);

                            col.Item().PaddingBottom(15).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    for (int i = 0; i < colsCount; i++) columns.RelativeColumn();
                                });

                                if (tableSec.Headers.Count > 0)
                                {
                                    table.Header(header =>
                                    {
                                        for (int i = 0; i < tableSec.Headers.Count; i++)
                                        {
                                            var headerBg = ParseColor(tableSec.HeaderColor, Colors.Grey.Lighten3);
                                            header.Cell().Background(headerBg).Padding(6).Text(tableSec.Headers[i]).Bold().FontSize(10);
                                        }
                                    });
                                }

                                for (int r = 0; r < tableSec.Rows.Count; r++)
                                {
                                    var rowVals = tableSec.Rows[r];
                                    string bg = tableSec.Theme == "dark"
                                        ? (r % 2 == 0 ? Colors.Grey.Lighten3 : Colors.Grey.Lighten2)
                                        : (r % 2 == 0 ? Colors.White : Colors.Grey.Lighten4);

                                    for (int c = 0; c < colsCount; c++)
                                    {
                                        string val = c < rowVals.Count ? rowVals[c] : "";
                                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(val).FontSize(10);
                                    }
                                }
                            });
                        }
                        else if (section is PdfBuilderContext.ViewSection viewSec)
                        {
                            // Renderiza o conteúdo textual puro da View/HTML com formatação limpa
                            string sanitized = System.Text.RegularExpressions.Regex.Replace(viewSec.HtmlContent, "<.*?>", string.Empty);
                            col.Item().PaddingBottom(10).Text(sanitized.Trim()).FontSize(11).LineHeight(1.4f);
                        }
                    }
                });

                // Rodapé
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text(ctx.Footer ?? "Yvira Platform Engine").FontSize(9).FontColor(Colors.Grey.Darken1);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                    });
                });
            });
        });

        document.GeneratePdf(fullPath);
    }

    private static string ParseColor(string? hex, string defaultColor)
    {
        if (string.IsNullOrWhiteSpace(hex)) return defaultColor;
        if (!hex.StartsWith("#")) hex = "#" + hex;
        return hex;
    }

    private static void SetCellValue(IXLCell cell, object? val)
    {
        if (val == null) cell.Value = "";
        else if (val is double d) cell.Value = d;
        else if (val is int i) cell.Value = (double)i;
        else if (val is bool b) cell.Value = b;
        else cell.Value = val.ToString();
    }
}
