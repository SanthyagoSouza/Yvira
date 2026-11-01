using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Yvira.Vm;

public static class DocumentModule
{
    static DocumentModule()
    {
        // Configura a licença gratuita comunitária da QuestPDF
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
        // doc.writeExcel(caminho, { title: "Vendas", headers: ["ID", "Produto", "Valor"], rows: [[1, "Teclado", 150.0], [2, "Mouse", 80.0]] })
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
                // Título opcional
                if (map.Get("title") is string titleText)
                {
                    ws.Cell(currentRow, 1).Value = titleText;
                    ws.Cell(currentRow, 1).Style.Font.Bold = true;
                    ws.Cell(currentRow, 1).Style.Font.FontSize = 14;
                    currentRow += 2;
                }

                // Cabeçalhos
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

                // Linhas de Dados
                if (map.Get("rows") is YviraList rows)
                {
                    for (int r = 0; r < rows.Count; r++)
                    {
                        var rowObj = rows.Get(r);
                        if (rowObj is YviraList rowList)
                        {
                            for (int c = 0; c < rowList.Count; c++)
                            {
                                var val = rowList.Get(c);
                                SetCellValue(ws.Cell(currentRow, c + 1), val);
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
                // Exportação direta de Matriz Científica para Excel com formatação
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
                // Lista de Listas direta
                for (int r = 0; r < rawTable.Count; r++)
                {
                    if (rawTable.Get(r) is YviraList rowList)
                    {
                        for (int c = 0; c < rowList.Count; c++)
                        {
                            SetCellValue(ws.Cell(r + 1, c + 1), rowList.Get(c));
                        }
                    }
                }
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(fullPath);
            return true;
        });

        // doc.readExcel(caminho, sheetName?) -> YviraList de YviraLists
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
        // 2. GERAÇÃO DE RELATÓRIOS E DOCUMENTOS PDF
        // ==========================================
        // doc.writePdf(caminho, { title: "Relatório Financeiro", subtitle: "Outubro 2026", headers: [...], rows: [...], footer: "Gerado por Yvira v2.1" })
        methods["writePdf"] = new HostFunction("doc.writePdf", (interp, args) =>
        {
            if (args.Count < 2 || args[0] == null || args[1] == null)
                throw new Exception("doc.writePdf requer (caminho, dados).");

            string relativePath = args[0]!.ToString()!;
            string fullPath = Path.IsPathRooted(relativePath) ? relativePath : Path.GetFullPath(Path.Combine(baseDirectory, relativePath));

            string? dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (args[1] is not YviraMap config)
                throw new Exception("doc.writePdf requer um mapa/objeto com as configurações do documento.");

            string title = config.Get("title")?.ToString() ?? "Documento Yvira";
            string? subtitle = config.Get("subtitle")?.ToString();
            string? text = config.Get("content")?.ToString() ?? config.Get("text")?.ToString();
            string? footer = config.Get("footer")?.ToString();

            YviraList? headers = config.Get("headers") as YviraList;
            YviraList? rows = config.Get("rows") as YviraList;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Segoe UI", "Arial", "Helvetica"));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Text(title).Bold().FontSize(22).FontColor(Colors.Blue.Darken2);
                        if (!string.IsNullOrEmpty(subtitle))
                        {
                            col.Item().Text(subtitle).FontSize(13).FontColor(Colors.Grey.Medium);
                        }
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    // Conteúdo
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        if (!string.IsNullOrEmpty(text))
                        {
                            col.Item().PaddingBottom(15).Text(text).FontSize(11).LineHeight(1.4f);
                        }

                        // Tabela estilizada (se houver linhas)
                        if (rows != null && rows.Count > 0)
                        {
                            int colsCount = headers != null && headers.Count > 0
                                ? headers.Count
                                : (rows.Get(0) is YviraList r0 ? r0.Count : 1);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    for (int i = 0; i < colsCount; i++)
                                    {
                                        columns.RelativeColumn();
                                    }
                                });

                                // Cabeçalhos da tabela
                                if (headers != null)
                                {
                                    table.Header(header =>
                                    {
                                        for (int i = 0; i < headers.Count; i++)
                                        {
                                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text(headers.Get(i)?.ToString() ?? "").Bold();
                                        }
                                    });
                                }

                                // Linhas da tabela
                                for (int r = 0; r < rows.Count; r++)
                                {
                                    var rowItem = rows.Get(r);
                                    string bg = (r % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                                    if (rowItem is YviraList rowValues)
                                    {
                                        for (int c = 0; c < colsCount; c++)
                                        {
                                            string cellText = c < rowValues.Count ? (rowValues.Get(c)?.ToString() ?? "") : "";
                                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(cellText);
                                        }
                                    }
                                }
                            });
                        }
                    });

                    // Rodapé
                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text(footer ?? "Gerado nativamente por Yvira Engine").FontSize(9).FontColor(Colors.Grey.Darken1);
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
            return true;
        });

        return new YviraMap(methods);
    }

    private static void SetCellValue(IXLCell cell, object? val)
    {
        if (val == null)
        {
            cell.Value = "";
        }
        else if (val is double d)
        {
            cell.Value = d;
        }
        else if (val is int i)
        {
            cell.Value = (double)i;
        }
        else if (val is bool b)
        {
            cell.Value = b;
        }
        else
        {
            cell.Value = val.ToString();
        }
    }
}
