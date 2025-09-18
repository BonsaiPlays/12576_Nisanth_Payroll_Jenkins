using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollApi.Data;
using PayrollApi.Utils;

namespace PayrollApi.Controllers
{
    [ApiController]
    [Route("api/audit")]
    [Authorize(Roles = "Admin,HRManager")]
    public class AuditController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuditController(AppDbContext db) => _db = db;

        private bool IsAdmin => User.IsInRole("Admin");
        private bool IsHRManager => User.IsInRole("HRManager");

        /// <summary>
        /// Retrieves audit logs within a date range, with optional entity type filtering and role-based access control.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] string? entityType = null
        )
        {
            var endOfDay = to.Date.AddDays(1).AddTicks(-1);
            var q = _db.AuditLogs.Where(l =>
                l.PerformedAt >= from.Date && l.PerformedAt <= endOfDay
            );

            if (IsAdmin && !IsHRManager)
            {
                q = q.Where(l => l.EntityType == "User" || l.EntityType == "EmployeeProfile");
            }
            else if (IsHRManager && !IsAdmin)
            {
                q = q.Where(l => l.EntityType == "Payslip" || l.EntityType == "CTCStructure");
            }

            if (!string.IsNullOrEmpty(entityType))
                q = q.Where(l => l.EntityType == entityType);

            var logs = await q.OrderByDescending(l => l.PerformedAt).ToListAsync();
            return Ok(new { Total = logs.Count, Items = logs });
        }

        /// <summary>
        /// Exports audit logs within a date range to Excel, with role-based filtering and optional entity type restriction.
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportExcel(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] string? entityType = null
        )
        {
            try
            {
                var start = from.Date;
                var end = to.Date.AddDays(1).AddTicks(-1);

                var q = _db.AuditLogs.Where(l => l.PerformedAt >= start && l.PerformedAt <= end);

                if (IsAdmin && !IsHRManager)
                {
                    q = q.Where(l => l.EntityType == "User" || l.EntityType == "EmployeeProfile");
                }
                else if (IsHRManager && !IsAdmin)
                {
                    q = q.Where(l => l.EntityType == "Payslip" || l.EntityType == "CTCStructure");
                }

                if (!string.IsNullOrEmpty(entityType))
                    q = q.Where(l => l.EntityType == entityType);

                var logs = await q.OrderByDescending(l => l.PerformedAt).ToListAsync();

                if (logs.Count == 0)
                {
                    return BadRequest("No audit logs found for the selected date range.");
                }

                var bytes = AuditExporter.ExportAuditLogs(logs);

                if (bytes == null || bytes.Length == 0)
                {
                    return StatusCode(500, "Failed to generate Excel file.");
                }

                var fileNamePrefix = IsAdmin ? "AdminAudit" : "HRAudit";
                var fname = $"{fileNamePrefix}_{from:dd-MM-yyyy}_{to:dd-MM-yyyy}.xlsx";

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fname
                );
            }
            catch (Exception ex)
            {
                // Log it
                Console.WriteLine($"EXPORT FAILED: {ex}");
                return StatusCode(500, $"Export failed: {ex.Message}");
            }
        }
    }
}
