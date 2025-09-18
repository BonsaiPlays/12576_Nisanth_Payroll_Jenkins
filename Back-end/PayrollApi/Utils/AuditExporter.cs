using OfficeOpenXml;
using PayrollApi.Models;

namespace PayrollApi.Utils
{
    public static class AuditExporter
    {
        public static byte[] ExportAuditLogs(IEnumerable<AuditLog> logs)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("AuditLogs");

            var headers = new[] { "EntityType", "Action", "Details", "PerformedBy", "PerformedAt" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cells[1, i + 1].Value = headers[i];

            int row = 2;
            foreach (var log in logs)
            {
                ws.Cells[row, 1].Value = log.EntityType;
                ws.Cells[row, 2].Value = log.Action;
                ws.Cells[row, 3].Value = log.Details;
                ws.Cells[row, 4].Value = log.PerformedBy;
                ws.Cells[row, 5].Value = log.PerformedAt.ToString("yyyy-MM-dd HH:mm:ss");
                row++;
            }

            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }
    }
}
