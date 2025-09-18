using PayrollApi.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PayrollApi.Utils
{
    public static class PdfExporter
    {
        private static string FormatMoney(decimal value) => $"₹{value:n2}";

        public static byte[] GeneratePayslipPdf(Payslip slip, string employeeName)
        {
            var totalAllowances = slip.AllowanceItems.Sum(a => a.Amount) + slip.Basic + slip.HRA;
            var totalDeductions = slip.DeductionItems.Sum(d => d.Amount) + slip.TaxDeducted;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    // Header
                    page.Header()
                        .AlignCenter()
                        .Text($"Payslip - {slip.Month:00}/{slip.Year}")
                        .FontSize(20)
                        .SemiBold()
                        .FontColor(Colors.Blue.Medium);

                    page.Content()
                        .Stack(stack =>
                        {
                            // Employee section
                            stack
                                .Item()
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .Text("Employee Info")
                                .Bold();
                            stack
                                .Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(160);
                                        c.RelativeColumn();
                                    });

                                    void Info(string label, string value)
                                    {
                                        table.Cell().Padding(3).Text(label).SemiBold();
                                        table.Cell().Padding(3).Text(value);
                                    }

                                    Info("Employee", employeeName);
                                    Info("Year / Month", $"{slip.Year}/{slip.Month:00}");
                                });

                            // Earnings
                            stack
                                .Item()
                                .PaddingTop(10)
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .Text("Earnings / Allowances")
                                .Bold();
                            stack
                                .Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn();
                                        c.ConstantColumn(140);
                                    });

                                    void Row(string label, string value)
                                    {
                                        table.Cell().Padding(4).Text(label);
                                        table.Cell().Padding(4).AlignRight().Text(value);
                                    }

                                    Row("Basic", FormatMoney(slip.Basic));
                                    Row("HRA", FormatMoney(slip.HRA));
                                    foreach (var a in slip.AllowanceItems)
                                        Row(a.Label, FormatMoney(a.Amount));

                                    table
                                        .Cell()
                                        .ColumnSpan(2)
                                        .PaddingTop(4)
                                        .BorderTop(1)
                                        .Text($"Total Earnings: {FormatMoney(totalAllowances)}")
                                        .Bold()
                                        .AlignRight();
                                });

                            // Deductions
                            stack
                                .Item()
                                .PaddingTop(10)
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .Text("Deductions")
                                .Bold();
                            stack
                                .Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn();
                                        c.ConstantColumn(140);
                                    });

                                    void Row(string label, string value)
                                    {
                                        table.Cell().Padding(4).Text(label);
                                        table.Cell().Padding(4).AlignRight().Text(value);
                                    }

                                    foreach (var d in slip.DeductionItems)
                                        Row(d.Label, FormatMoney(d.Amount));

                                    Row("Tax Deducted", FormatMoney(slip.TaxDeducted));
                                    Row("LOP Days", slip.LOPDays.ToString());

                                    table
                                        .Cell()
                                        .ColumnSpan(2)
                                        .PaddingTop(4)
                                        .BorderTop(1)
                                        .Text($"Total Deductions: {FormatMoney(totalDeductions)}")
                                        .Bold()
                                        .AlignRight();
                                });

                            // Net Pay Summary
                            stack
                                .Item()
                                .PaddingTop(15)
                                .Border(1)
                                .Background(Colors.Blue.Lighten5)
                                .Padding(10)
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.Span("Net Pay: ").SemiBold().FontSize(14);
                                    t.Span(FormatMoney(slip.NetPay))
                                        .Bold()
                                        .FontColor(Colors.Green.Medium)
                                        .FontSize(16);
                                });
                        });

                    page.Footer()
                        .AlignRight()
                        .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            return doc.GeneratePdf();
        }

        public static byte[] GenerateCtcPdf(CTCStructure ctc, string employeeName)
        {
            var totalAllowances = ctc.Allowances.Sum(a => a.Amount) + ctc.Basic + ctc.HRA;
            var totalDeductions = ctc.Deductions.Sum(d => d.Amount);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    // Header
                    page.Header()
                        .AlignCenter()
                        .Text($"CTC Structure - {employeeName}")
                        .FontSize(20)
                        .SemiBold()
                        .FontColor(Colors.Blue.Medium);

                    page.Content()
                        .Stack(stack =>
                        {
                            // Info
                            stack
                                .Item()
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .Text("Employee Info")
                                .Bold();
                            stack
                                .Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(160);
                                        c.RelativeColumn();
                                    });
                                    void Info(string label, string value)
                                    {
                                        table.Cell().Padding(3).Text(label).SemiBold();
                                        table.Cell().Padding(3).Text(value);
                                    }
                                    Info("Employee", employeeName);
                                    Info("Effective From", $"{ctc.EffectiveFrom:yyyy-MM-dd}");
                                    Info("Status", ctc.Status.ToString());
                                });

                            // Allowances
                            stack
                                .Item()
                                .PaddingTop(10)
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .Text("Earnings / Allowances")
                                .Bold();
                            stack
                                .Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn();
                                        c.ConstantColumn(140);
                                    });
                                    void Row(string label, string value)
                                    {
                                        table.Cell().Padding(4).Text(label);
                                        table.Cell().Padding(4).AlignRight().Text(value);
                                    }

                                    Row("Basic", FormatMoney(ctc.Basic));
                                    Row("HRA", FormatMoney(ctc.HRA));
                                    foreach (var a in ctc.Allowances)
                                        Row(a.Label, FormatMoney(a.Amount));

                                    table
                                        .Cell()
                                        .ColumnSpan(2)
                                        .PaddingTop(4)
                                        .BorderTop(1)
                                        .Text($"Total Earnings: {FormatMoney(totalAllowances)}")
                                        .Bold()
                                        .AlignRight();
                                });

                            // Deductions
                            stack
                                .Item()
                                .PaddingTop(10)
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .Text("Deductions")
                                .Bold();
                            stack
                                .Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn();
                                        c.ConstantColumn(140);
                                    });
                                    void Row(string label, string value)
                                    {
                                        table.Cell().Padding(4).Text(label);
                                        table.Cell().Padding(4).AlignRight().Text(value);
                                    }

                                    foreach (var d in ctc.Deductions)
                                        Row(d.Label, FormatMoney(d.Amount));

                                    Row("Tax Percent", $"{ctc.TaxPercent:n2}%");

                                    table
                                        .Cell()
                                        .ColumnSpan(2)
                                        .PaddingTop(4)
                                        .BorderTop(1)
                                        .Text($"Total Deductions: {FormatMoney(totalDeductions)}")
                                        .Bold()
                                        .AlignRight();
                                });

                            // Gross CTC
                            stack
                                .Item()
                                .PaddingTop(15)
                                .Border(1)
                                .Background(Colors.Blue.Lighten5)
                                .Padding(10)
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.Span("Gross CTC: ").SemiBold().FontSize(14);
                                    t.Span(FormatMoney(ctc.GrossCTC))
                                        .Bold()
                                        .FontColor(Colors.Green.Medium)
                                        .FontSize(16);
                                });
                        });

                    page.Footer()
                        .AlignRight()
                        .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            return doc.GeneratePdf();
        }
    }
}
