using System.Globalization;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PayrollApi.Models;

namespace PayrollApi.Utils
{
    public static class ExcelExporter
    {
        /// <summary>
        /// Exports given payslips into an Excel worksheet and returns the file bytes.
        /// </summary>
        public static byte[] ExportPayslips(IEnumerable<Payslip> slips)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Payslips");

            if (!slips.Any())
                return package.GetAsByteArray();

            var empName = slips.First().EmployeeProfile?.User?.FullName ?? "Employee";

            // Title Row
            ws.Cells[1, 1].Value = $"{empName} – Payslips";
            ws.Cells[1, 1, 1, 12].Merge = true;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;

            var headers = new[]
            {
                "EmployeeProfileId",
                "Year",
                "Month",
                $"Basic (₹)",
                $"HRA (₹)",
                $"Total Allowances (₹)",
                $"Total Deductions (₹)",
                $"Tax (₹)",
                "LOP Days",
                $"NetPay (₹)",
                "Approved",
                "Released",
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cells[2, i + 1].Value = headers[i];

            int row = 3;
            foreach (var s in slips)
            {
                var totalAllowances = s.AllowanceItems.Sum(a => a.Amount);
                var totalDeductions = s.DeductionItems.Sum(d => d.Amount);

                ws.Cells[row, 1].Value = s.EmployeeProfileId;
                ws.Cells[row, 2].Value = s.Year;
                ws.Cells[row, 3].Value = s.Month;

                ws.Cells[row, 4].Value = Math.Round(s.Basic, 2);
                ws.Cells[row, 5].Value = Math.Round(s.HRA, 2);
                ws.Cells[row, 6].Value = Math.Round(totalAllowances, 2);
                ws.Cells[row, 7].Value = Math.Round(totalDeductions, 2);
                ws.Cells[row, 8].Value = Math.Round(s.TaxDeducted, 2);
                ws.Cells[row, 9].Value = s.LOPDays;
                ws.Cells[row, 10].Value = Math.Round(s.NetPay, 2);

                ws.Cells[row, 11].Value = s.Status.ToString();
                ws.Cells[row, 12].Value = s.IsReleased ? "Yes" : "No";

                // Format numeric amount cells as rupee
                ws.Cells[row, 4, row, 8].Style.Numberformat.Format = "₹#,##0.00";
                ws.Cells[row, 10].Style.Numberformat.Format = "₹#,##0.00";

                row++;
            }

            // Bold headers
            using var range = ws.Cells[2, 1, 2, headers.Length];
            range.Style.Font.Bold = true;

            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        /// <summary>
        /// Exports given CTC structures into Excel and returns file bytes.
        /// </summary>
        public static byte[] ExportCTCs(IEnumerable<CTCStructure> ctcs)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("CTCs");

            if (!ctcs.Any())
                return package.GetAsByteArray();

            var empName = ctcs.First().EmployeeProfile?.User?.FullName ?? "Employee";

            // Title Row
            ws.Cells[1, 1].Value = $"{empName} – CTCs";
            ws.Cells[1, 1, 1, 8].Merge = true;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;

            var headers = new[]
            {
                "EffectiveFrom",
                $"Basic (₹)",
                $"HRA (₹)",
                $"GrossCTC (₹)",
                $"TaxPercent (%)",
                "Status",
                $"Allowances (₹)",
                $"Deductions (₹)",
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cells[2, i + 1].Value = headers[i];

            int row = 3;
            foreach (var c in ctcs)
            {
                ws.Cells[row, 1].Value = c.EffectiveFrom.ToString("yyyy-MM-dd");
                ws.Cells[row, 2].Value = Math.Round(c.Basic, 2);
                ws.Cells[row, 3].Value = Math.Round(c.HRA, 2);
                ws.Cells[row, 4].Value = Math.Round(c.GrossCTC, 2);
                ws.Cells[row, 5].Value = Math.Round(c.TaxPercent, 2);
                ws.Cells[row, 6].Value = c.Status.ToString();
                ws.Cells[row, 7].Value = string.Join(
                    ", ",
                    c.Allowances.Select(a => $"{a.Label}:{a.Amount:F2}")
                );
                ws.Cells[row, 8].Value = string.Join(
                    ", ",
                    c.Deductions.Select(d => $"{d.Label}:{d.Amount:F2}")
                );

                // Apply rupee formatting
                ws.Cells[row, 2].Style.Numberformat.Format = "₹#,##0.00";
                ws.Cells[row, 3].Style.Numberformat.Format = "₹#,##0.00";
                ws.Cells[row, 4].Style.Numberformat.Format = "₹#,##0.00";

                row++;
            }

            // Add summary row below data
            var approvedCount = ctcs.Count(c => c.Status == Models.Enums.ApprovalStatus.Approved);
            var pendingCount = ctcs.Count(c => c.Status == Models.Enums.ApprovalStatus.Pending);
            var rejectedCount = ctcs.Count(c => c.Status == Models.Enums.ApprovalStatus.Rejected);

            ws.Cells[row + 1, 1].Value = "Summary";
            ws.Cells[row + 1, 1].Style.Font.Bold = true;

            ws.Cells[row + 1, 2].Value = $"Approved: {approvedCount}";
            ws.Cells[row + 1, 3].Value = $"Pending: {pendingCount}";
            ws.Cells[row + 1, 4].Value = $"Rejected: {rejectedCount}";

            using var range = ws.Cells[2, 1, 2, headers.Length];
            range.Style.Font.Bold = true;

            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }
    }
}
