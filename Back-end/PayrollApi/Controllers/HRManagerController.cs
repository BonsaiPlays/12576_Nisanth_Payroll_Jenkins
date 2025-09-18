using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollApi.Data;
using PayrollApi.Models;
using PayrollApi.Models.Enums;
using PayrollApi.Services;

namespace PayrollApi.Controllers
{
    [ApiController]
    [Route("api/hr-manager")]
    [Authorize(Roles = "HRManager,Admin")]
    public class HRManagerController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;
        private readonly IAuditService _audit;

        public HRManagerController(AppDbContext db, IEmailService email, IAuditService audit)
        {
            _db = db;
            _email = email;
            _audit = audit;
        }

        /// <summary>
        /// Gets the current user's ID from the authenticated claims.
        /// </summary>
        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Gets the current user's email from the authenticated claims, defaulting to "system" if not found.
        /// </summary>
        private string CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email) ?? "system";

        /// <summary>
        /// Approves the latest pending CTC structure for the specified employee and notifies them via email and notification.
        /// </summary>
        [HttpPost("ctc/{employeeUserId}/approve")]
        public async Task<IActionResult> ApproveCTC(int employeeUserId)
        {
            var profile = await _db
                .EmployeeProfiles.Include(p => p.CTCStructures)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == employeeUserId);

            if (profile == null)
                return NotFound("Employee profile not found");

            var latestPending = profile
                .CTCStructures.OrderByDescending(c => c.EffectiveFrom)
                .FirstOrDefault(c => !c.IsApproved);

            if (latestPending == null)
                return NotFound("No pending CTC found to approve");

            latestPending.IsApproved = true;
            latestPending.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            if (latestPending?.CreatedByUserId > 0)
            {
                var creator = await _db.Users.FindAsync(latestPending.CreatedByUserId);
                if (creator != null)
                {
                    //Notification
                    _db.Notifications.Add(
                        new Notification
                        {
                            UserId = creator.Id,
                            Subject = "CTC Status Updated",
                            Message =
                                $"The CTC you created for {profile.User.FullName} effective {latestPending.EffectiveFrom:dd-MMM-yyyy} was approved.",
                        }
                    );

                    //Mail
                    await _email.SendTemplatedAsync(
                        creator.Email,
                        "CTC Status Updated",
                        $"Hello {creator.FullName},<br/>The CTC you created for {profile.User.FullName} was <b>approved</b>."
                    );
                }
                await _db.SaveChangesAsync();
            }

            // Logging
            await _audit.LogAsync(
                $"CTCStructure oh {latestPending.EmployeeProfile.User.FullName} ({latestPending.EmployeeProfile.User.FullName})",
                latestPending.Id,
                "Approved",
                $"CTC eff {latestPending.EffectiveFrom:yyyy-MM-dd} approved for {profile.User.Email}",
                CurrentUserId,
                CurrentUserEmail
            );

            return Ok(new { Message = "CTC approved" });
        }

        /// <summary>
        /// Approves a payslip and notifies the employee via email and in-app notification.
        /// </summary>
        [HttpPost("payslips/{payslipId}/approve")]
        public async Task<IActionResult> ApprovePayslip(int payslipId)
        {
            var slip = await _db
                .Payslips.Include(p => p.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .FirstOrDefaultAsync(p => p.Id == payslipId);

            if (slip == null)
                return NotFound();

            // Check if there's already a released payslip for this month
            var existingReleased = await _db.Payslips.AnyAsync(p =>
                p.EmployeeProfileId == slip.EmployeeProfileId
                && p.Year == slip.Year
                && p.Month == slip.Month
                && p.IsReleased
            );

            if (existingReleased)
                return BadRequest(
                    "Cannot approve payslip - a released payslip already exists for this month"
                );

            slip.Status = ApprovalStatus.Approved;
            slip.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Logging
            await _audit.LogAsync(
                "Payslip",
                slip.Id,
                "Approved",
                $"Payslip {slip.Month}/{slip.Year} approved for {slip.EmployeeProfile.User.FullName} ({slip.EmployeeProfile.User.Email})",
                CurrentUserId,
                CurrentUserEmail
            );

            if (slip.CreatedByUserId > 0)
            {
                var creator = await _db.Users.FindAsync(slip.CreatedByUserId);
                if (creator != null)
                {
                    //Notification
                    _db.Notifications.Add(
                        new Notification
                        {
                            UserId = creator.Id,
                            Subject = "Payslip Status Updated",
                            Message =
                                $"The payslip you created for {slip.EmployeeProfile.User.FullName} ({slip.Month}/{slip.Year}) was approved.",
                        }
                    );

                    //Mail
                    await _email.SendTemplatedAsync(
                        creator.Email,
                        "Payslip Status Updated",
                        $"Hello {creator.FullName},<br/>The payslip you created for {slip.EmployeeProfile.User.FullName} ({slip.Month}/{slip.Year}) was <b>approved</b>."
                    );
                }
                await _db.SaveChangesAsync();
            }

            return Ok(new { Message = "Payslip approved" });
        }

        /// <summary>
        /// Releases an approved payslip and notifies the employee via email and in-app notification.
        /// </summary>
        [HttpPost("payslips/{payslipId}/release")]
        public async Task<IActionResult> ReleasePayslip(int payslipId)
        {
            var slip = await _db
                .Payslips.Include(p => p.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .FirstOrDefaultAsync(p => p.Id == payslipId);

            if (slip == null)
                return NotFound();

            // Check if there's already a released payslip for this month
            var existingReleased = await _db.Payslips.AnyAsync(p =>
                p.EmployeeProfileId == slip.EmployeeProfileId
                && p.Year == slip.Year
                && p.Month == slip.Month
                && p.IsReleased
                && p.Id != slip.Id
            );

            if (existingReleased)
                return BadRequest("A payslip for this month has already been released");

            if (slip.Status != ApprovalStatus.Approved)
                return BadRequest("Payslip must be approved before release");

            // Mark all other approved payslips for this month as rejected
            var otherApprovedPayslips = await _db
                .Payslips.Where(p =>
                    p.EmployeeProfileId == slip.EmployeeProfileId
                    && p.Year == slip.Year
                    && p.Month == slip.Month
                    && p.Status == ApprovalStatus.Approved
                    && p.Id != slip.Id
                )
                .ToListAsync();

            foreach (var other in otherApprovedPayslips)
            {
                other.Status = ApprovalStatus.Rejected;
                other.UpdatedAt = DateTime.UtcNow;

                await _audit.LogAsync(
                    "Payslip",
                    other.Id,
                    "AutoReject",
                    $"Payslip {other.Id} rejected automatically because payslip {slip.Id} was released for {slip.Month}/{slip.Year}",
                    CurrentUserId,
                    CurrentUserEmail
                );
            }

            slip.IsReleased = true;
            slip.UpdatedAt = DateTime.UtcNow;

            await _audit.LogAsync(
                "Payslip",
                slip.Id,
                "Released",
                $"Payslip {slip.Month}/{slip.Year} released for {slip.EmployeeProfile.User.Email}",
                CurrentUserId,
                CurrentUserEmail
            );

            // Notification
            _db.Notifications.Add(
                new Notification
                {
                    UserId = slip.EmployeeProfile.User.Id,
                    Subject = "New Payslip Released",
                    Message = $"Payslip for {slip.Month}/{slip.Year} is now available.",
                }
            );
            await _db.SaveChangesAsync();

            // Mail
            await _email.SendTemplatedAsync(
                slip.EmployeeProfile.User.Email,
                "Payslip Released",
                $"Hello {slip.EmployeeProfile.User.FullName},<br/>Your payslip for {slip.Month}/{slip.Year} has been released. You can now view and download it."
            );

            return Ok(new { Message = "Payslip released and employee notified" });
        }

        /// <summary>
        /// Updates the approval status of a CTC structure and rejects other active CTCs if setting to Approved.
        /// </summary>
        [HttpPost("ctcs/{ctcId}/status")]
        public async Task<IActionResult> SetCTCStatus(int ctcId, [FromQuery] ApprovalStatus status)
        {
            var ctc = await _db
                .CTCStructures.Include(c => c.EmployeeProfile)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == ctcId);

            if (ctc == null)
                return NotFound("CTC not found");

            if (status == ApprovalStatus.Approved)
            {
                // Period validity: 1 full year from EffectiveFrom
                ctc.EffectiveTo = ctc.EffectiveFrom.AddYears(1);

                // Check yearly uniqueness
                bool yearExists = await _db.CTCStructures.AnyAsync(c =>
                    c.EmployeeProfileId == ctc.EmployeeProfileId
                    && c.Id != ctc.Id
                    && c.Status == ApprovalStatus.Approved
                    && c.EffectiveFrom.Year == ctc.EffectiveFrom.Year
                );

                if (yearExists)
                    return Conflict(
                        new { message = "Employee already has an approved CTC in this year." }
                    );

                // Check overlap conflicts
                bool overlaps = await _db.CTCStructures.AnyAsync(c =>
                    c.EmployeeProfileId == ctc.EmployeeProfileId
                    && c.Id != ctc.Id
                    && c.Status == ApprovalStatus.Approved
                    && c.EffectiveFrom < ctc.EffectiveTo
                    && c.EffectiveTo > ctc.EffectiveFrom
                );

                if (overlaps)
                    return Conflict(
                        new { message = "Another approved CTC overlaps with this time period." }
                    );

                // Approve this CTC
                ctc.Status = ApprovalStatus.Approved;
                ctc.IsApproved = true;

                // Retroactive rule: reject newer effective CTCs
                var newerCtcs = await _db
                    .CTCStructures.Where(c =>
                        c.EmployeeProfileId == ctc.EmployeeProfileId
                        && c.Id != ctc.Id
                        && c.Status == ApprovalStatus.Approved
                        && c.EffectiveFrom > ctc.EffectiveFrom
                    )
                    .ToListAsync();

                foreach (var newer in newerCtcs)
                {
                    newer.Status = ApprovalStatus.Rejected;
                    newer.IsApproved = false;
                    newer.UpdatedAt = DateTime.UtcNow;

                    await _audit.LogAsync(
                        "CTCStructure",
                        newer.Id,
                        "AutoReject",
                        $"CTC {newer.Id} rejected automatically because retroactive CTC {ctc.Id} effective {ctc.EffectiveFrom:yyyy-MM-dd} was approved.",
                        CurrentUserId,
                        CurrentUserEmail
                    );
                }
            }
            else
            {
                // Rejection or Pending
                ctc.Status = status;
                ctc.IsApproved = false;
            }

            ctc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Audit log
            await _audit.LogAsync(
                "CTCStructure",
                ctc.Id,
                "StatusChanged",
                $"CTC marked {status} for employee {ctc.EmployeeProfile.User.FullName} ({ctc.EmployeeProfile.User.Email})",
                CurrentUserId,
                CurrentUserEmail
            );

            // Notify employee
            _db.Notifications.Add(
                new Notification
                {
                    UserId = ctc.EmployeeProfile.UserId,
                    Subject = "CTC Status Updated",
                    Message =
                        $"Your CTC effective {ctc.EffectiveFrom:dd-MMM-yyyy} was marked as {status}.",
                }
            );
            await _db.SaveChangesAsync();

            await _email.SendTemplatedAsync(
                ctc.EmployeeProfile.User.Email,
                "CTC Status Updated",
                $"Hello {ctc.EmployeeProfile.User.FullName},<br/>Your CTC effective {ctc.EffectiveFrom:dd-MMM-yyyy} was set to <b>{status}</b>."
            );

            // HR creator notification (optional)
            if (ctc.CreatedByUserId > 0)
            {
                var creator = await _db.Users.FindAsync(ctc.CreatedByUserId);
                if (creator != null)
                {
                    _db.Notifications.Add(
                        new Notification
                        {
                            UserId = creator.Id,
                            Subject = "CTC Review Completed",
                            Message =
                                $"The CTC you created for {ctc.EmployeeProfile.User.FullName} effective {ctc.EffectiveFrom:dd-MMM-yyyy} was {status}.",
                        }
                    );
                    await _db.SaveChangesAsync();

                    await _email.SendTemplatedAsync(
                        creator.Email,
                        "CTC Review Completed",
                        $"Hello {creator.FullName},<br/>The CTC you created for {ctc.EmployeeProfile.User.FullName} effective {ctc.EffectiveFrom:dd-MMM-yyyy} was <b>{status}</b>."
                    );
                }
            }

            return Ok(new { Message = $"CTC id={ctc.Id} marked as {status}" });
        }

        /// <summary>
        /// Updates the approval status of a payslip and notifies the employee via email and notification.
        /// </summary>
        [HttpPost("payslips/{payslipId}/status")]
        public async Task<IActionResult> SetPayslipStatus(
            int payslipId,
            [FromQuery] ApprovalStatus status
        )
        {
            var slip = await _db
                .Payslips.Include(p => p.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .FirstOrDefaultAsync(p => p.Id == payslipId);

            if (slip == null)
                return NotFound();

            slip.Status = status;
            slip.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Logging
            await _audit.LogAsync(
                $"Payslip of {slip.EmployeeProfile.User.FullName} ({slip.EmployeeProfile.User.Email})",
                slip.Id,
                "StatusChanged",
                $"Payslip {slip.Month}/{slip.Year} marked as {status}",
                CurrentUserId,
                CurrentUserEmail
            );

            // Notify the employee
            _db.Notifications.Add(
                new Notification
                {
                    UserId = slip.EmployeeProfile.User.Id,
                    Subject = "Payslip Status Updated",
                    Message = $"Your payslip for {slip.Month}/{slip.Year} was marked as {status}.",
                }
            );
            await _db.SaveChangesAsync();

            await _email.SendTemplatedAsync(
                slip.EmployeeProfile.User.Email,
                "Payslip Status Updated",
                $"Hello {slip.EmployeeProfile.User.FullName},<br/>Your payslip for {slip.Month}/{slip.Year} was set to <b>{status}</b>."
            );

            // Notify the HR/HRM who created this payslip
            if (slip.CreatedByUserId > 0)
            {
                var creator = await _db.Users.FindAsync(slip.CreatedByUserId);
                if (creator != null)
                {
                    _db.Notifications.Add(
                        new Notification
                        {
                            UserId = creator.Id,
                            Subject = "Payslip Review Completed",
                            Message =
                                $"The payslip you created for {slip.EmployeeProfile.User.FullName} ({slip.Month}/{slip.Year}) was {status}.",
                        }
                    );
                    await _db.SaveChangesAsync();

                    await _email.SendTemplatedAsync(
                        creator.Email,
                        "Payslip Review Completed",
                        $"Hello {creator.FullName},<br/>The payslip you created for {slip.EmployeeProfile.User.FullName} ({slip.Month}/{slip.Year}) was <b>{status}</b>."
                    );
                }
            }

            return Ok(new { Message = $"Payslip marked as {status}" });
        }

        /// <summary>
        /// Returns payroll summary grouped by department for a given month and year.
        /// </summary>
        [HttpGet("analytics/monthly-summary")]
        public async Task<IActionResult> MonthlySummary([FromQuery] int year, [FromQuery] int month)
        {
            var q = _db
                .Payslips.Include(p => p.EmployeeProfile)
                .ThenInclude(ep => ep.Department)
                .Where(p => p.Year == year && p.Month == month);

            var byDept = await q.GroupBy(p =>
                    p.EmployeeProfile.Department != null
                        ? p.EmployeeProfile.Department.Name
                        : "Unassigned"
                )
                .Select(g => new
                {
                    Department = g.Key,
                    TotalNet = g.Sum(x => x.NetPay),
                    AverageNet = g.Average(x => x.NetPay),
                    Count = g.Count(),
                })
                .ToListAsync();

            return Ok(byDept);
        }

        /// <summary>
        /// Compares total net payroll between two periods and returns difference and percentage change.
        /// </summary>
        [HttpGet("analytics/compare")]
        public async Task<IActionResult> CompareMonths(
            [FromQuery] int year1,
            [FromQuery] int month1,
            [FromQuery] int year2,
            [FromQuery] int month2
        )
        {
            var a = await _db
                .Payslips.Where(p => p.Year == year1 && p.Month == month1)
                .SumAsync(p => p.NetPay);
            var b = await _db
                .Payslips.Where(p => p.Year == year2 && p.Month == month2)
                .SumAsync(p => p.NetPay);
            var diff = b - a;
            var pct = a == 0 ? 0 : (diff / a) * 100m;

            return Ok(
                new
                {
                    PeriodA = new
                    {
                        year1,
                        month1,
                        TotalNet = a,
                    },
                    PeriodB = new
                    {
                        year2,
                        month2,
                        TotalNet = b,
                    },
                    Difference = diff,
                    PercentChange = Math.Round(pct, 2),
                }
            );
        }

        /// <summary>
        /// Identifies payroll anomalies based on percentage change compared to previous month.
        /// </summary>
        [HttpGet("analytics/anomalies")]
        public async Task<IActionResult> Anomalies(
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] decimal thresholdPercent = 20m
        )
        {
            var current = await _db
                .Payslips.Include(p => p.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .Where(p => p.Year == year && p.Month == month)
                .ToListAsync();

            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear = month == 1 ? year - 1 : year;

            var prev = await _db
                .Payslips.Where(p => p.Year == prevYear && p.Month == prevMonth)
                .ToListAsync();

            var anomalies = current
                .Select(c =>
                {
                    var p = prev.FirstOrDefault(x => x.EmployeeProfileId == c.EmployeeProfileId);
                    if (p == null)
                        return new
                        {
                            c.Id,
                            c.EmployeeProfileId,
                            ChangePercent = 0m,
                            IsAnomaly = false,
                        };
                    var change = p.NetPay == 0 ? 0 : ((c.NetPay - p.NetPay) / p.NetPay) * 100m;
                    return new
                    {
                        c.Id,
                        c.EmployeeProfileId,
                        ChangePercent = Math.Round(change, 2),
                        IsAnomaly = Math.Abs(change) >= thresholdPercent,
                    };
                })
                .Where(x => x.IsAnomaly);

            return Ok(anomalies);
        }
    }
}
