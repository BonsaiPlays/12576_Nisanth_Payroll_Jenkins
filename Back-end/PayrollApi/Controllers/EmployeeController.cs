using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollApi.Data;
using PayrollApi.DTOs;
using PayrollApi.Models;
using PayrollApi.Models.Enums;
using PayrollApi.Services;
using PayrollApi.Utils;

namespace PayrollApi.Controllers
{
    [ApiController]
    [Route("api/employee")]
    [Authorize(Roles = "Employee,HR,HRManager,Admin")]
    public class EmployeeController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;
        private readonly IAuditService _audit;

        public EmployeeController(AppDbContext db, IEmailService email, IAuditService audit)
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
        /// Gets the current user's profile including department and latest approved CTC structure.
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var u = await _db
                .Users.Include(u => u.Profile)
                .ThenInclude(p => p.Department)
                .Include(u => u.Profile!.CTCStructures)
                .ThenInclude(c => c.Allowances)
                .Include(u => u.Profile!.CTCStructures)
                .ThenInclude(c => c.Deductions)
                .FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            if (u == null)
                return NotFound();

            // Get the latest approved CTC
            var latestCtc = u
                .Profile?.CTCStructures.Where(c => c.Status == ApprovalStatus.Approved)
                .OrderByDescending(c => c.EffectiveFrom)
                .FirstOrDefault();

            return Ok(
                new
                {
                    u.Id,
                    u.Email,
                    u.FullName,
                    Role = u.Role.ToString(),
                    Profile = u.Profile == null
                        ? null
                        : new
                        {
                            u.Profile.Address,
                            u.Profile.Phone,
                            Department = u.Profile.Department?.Name,
                            CTC = latestCtc == null
                                ? null
                                : new
                                {
                                    latestCtc.Basic,
                                    latestCtc.HRA,
                                    Allowances = latestCtc.Allowances.Select(a => new
                                    {
                                        a.Label,
                                        a.Amount,
                                    }),
                                    Deductions = latestCtc.Deductions.Select(d => new
                                    {
                                        d.Label,
                                        d.Amount,
                                    }),
                                    latestCtc.TaxPercent,
                                    latestCtc.GrossCTC,
                                    latestCtc.EffectiveFrom,
                                    latestCtc.IsApproved,
                                },
                        },
                }
            );
        }

        /// <summary>
        /// Updates the current user's employee profile and sends confirmation notification and email.
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(EmployeeProfileUpdate req)
        {
            var u = await _db
                .Users.Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            if (u?.Profile == null)
                return NotFound();

            u.Profile.Address = req.Address ?? u.Profile.Address;
            u.Profile.Phone = req.Phone ?? u.Profile.Phone;
            u.Profile.DepartmentId = req.DepartmentId ?? u.Profile.DepartmentId;
            u.Profile.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notification
            _db.Notifications.Add(
                new Notification
                {
                    UserId = u.Id,
                    Subject = "Profile Updated",
                    Message = "Your profile information has been updated successfully.",
                }
            );

            // Logging
            await _audit.LogAsync(
                "EmployeeProfile",
                u.Profile.Id,
                "Updated",
                $"Profile updated by {u.Email}",
                u.Id,
                u.Email
            );
            await _db.SaveChangesAsync();

            // Mail
            await _email.SendTemplatedAsync(
                u.Email,
                "Profile Updated",
                $"Hi {u.FullName},<br/>Your profile information has been updated successfully."
            );

            return Ok();
        }

        /// <summary>
        /// Retrieves a paginated list of payslips for the current user.
        /// </summary>
        [HttpGet("payslips")]
        public async Task<ActionResult<PagedResult<PayslipSummary>>> GetMyPayslips(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            var profileId = await _db
                .EmployeeProfiles.Where(p => p.UserId == CurrentUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (profileId == 0)
                return NotFound();

            var q = _db
                .Payslips.Where(p => p.EmployeeProfileId == profileId)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month);

            var (total, list) = (
                await q.CountAsync(),
                await q.Select(p => new PayslipSummary
                    {
                        Id = p.Id,
                        Year = p.Year,
                        Month = p.Month,
                        NetPay = p.NetPay,
                        Status = p.Status,
                        IsReleased = p.IsReleased,
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync()
            );

            return Ok(
                new PagedResult<PayslipSummary>
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    Items = list,
                }
            );
        }

        /// <summary>
        /// Downloads a PDF version of the specified payslip for the current user.
        /// </summary>
        [HttpGet("payslips/{payslipId}/pdf")]
        public async Task<IActionResult> DownloadMyPayslipPdf(int payslipId)
        {
            var profileId = await _db
                .EmployeeProfiles.Where(p => p.UserId == CurrentUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var slip = await _db
                .Payslips.Include(p => p.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .Include(p => p.AllowanceItems)
                .Include(p => p.DeductionItems)
                .FirstOrDefaultAsync(p => p.Id == payslipId && p.EmployeeProfileId == profileId);

            if (slip == null)
                return NotFound();

            var pdf = PdfExporter.GeneratePayslipPdf(slip, slip.EmployeeProfile.User.FullName);
            return File(pdf, "application/pdf", $"payslip_{slip.Year}_{slip.Month:00}.pdf");
        }

        /// <summary>
        /// Retrieves detailed information for a specific payslip belonging to the current user.
        /// </summary>
        [HttpGet("payslips/{payslipId}")]
        public async Task<ActionResult<PayslipResponse>> GetPayslipDetail(int payslipId)
        {
            var profileId = await _db
                .EmployeeProfiles.Where(p => p.UserId == CurrentUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (profileId == 0)
                return NotFound("Employee profile not found");

            var slip = await _db
                .Payslips.Include(p => p.AllowanceItems)
                .Include(p => p.DeductionItems)
                .FirstOrDefaultAsync(p => p.Id == payslipId && p.EmployeeProfileId == profileId);

            if (slip == null)
                return NotFound("Payslip not found");

            var response = new PayrollApi.DTOs.PayslipResponse
            {
                Id = slip.Id,
                Year = slip.Year,
                Month = slip.Month,
                Basic = slip.Basic,
                HRA = slip.HRA,
                AllowanceItems = slip
                    .AllowanceItems.Select(a => new PayrollApi.DTOs.PayslipItemDto
                    {
                        Label = a.Label,
                        Amount = a.Amount,
                    })
                    .ToList(),
                DeductionItems = slip
                    .DeductionItems.Select(d => new PayrollApi.DTOs.PayslipItemDto
                    {
                        Label = d.Label,
                        Amount = d.Amount,
                    })
                    .ToList(),
                TaxDeducted = slip.TaxDeducted,
                LOPDays = slip.LOPDays,
                NetPay = slip.NetPay,
                Status = slip.Status,
                IsReleased = slip.IsReleased,
            };

            return Ok(response);
        }

        /// <summary>
        /// Retrieves all CTC structures for the current user, ordered by effective date descending.
        /// </summary>
        [HttpGet("ctcs")]
        public async Task<IActionResult> GetMyCTCs()
        {
            var profileId = await _db
                .EmployeeProfiles.Where(p => p.UserId == CurrentUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (profileId == 0)
                return NotFound("Employee profile not found");

            var ctcs = await _db
                .CTCStructures.Include(c => c.Allowances)
                .Include(c => c.Deductions)
                .Where(c => c.EmployeeProfileId == profileId)
                .OrderByDescending(c => c.EffectiveFrom)
                .Select(c => new
                {
                    c.Id,
                    c.Basic,
                    c.HRA,
                    c.GrossCTC,
                    c.TaxPercent,
                    c.EffectiveFrom,
                    Status = c.Status.ToString(),
                    Allowances = c.Allowances.Select(a => new { a.Label, a.Amount }),
                    Deductions = c.Deductions.Select(d => new { d.Label, d.Amount }),
                })
                .ToListAsync();

            return Ok(ctcs);
        }

        /// <summary>
        /// Downloads a PDF version of the specified CTC structure for the current user.
        /// </summary>
        [HttpGet("ctcs/{ctcId}/pdf")]
        public async Task<IActionResult> DownloadMyCTCPdf(int ctcId)
        {
            var profileId = await _db
                .EmployeeProfiles.Where(p => p.UserId == CurrentUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var ctc = await _db
                .CTCStructures.Include(c => c.EmployeeProfile)
                .ThenInclude(p => p.User)
                .Include(c => c.Allowances)
                .Include(c => c.Deductions)
                .FirstOrDefaultAsync(c => c.Id == ctcId && c.EmployeeProfileId == profileId);

            if (ctc == null)
                return NotFound();

            var pdf = PdfExporter.GenerateCtcPdf(ctc, ctc.EmployeeProfile.User.FullName);
            return File(pdf, "application/pdf", $"ctc_{ctc.EffectiveFrom:yyyy_MM}_{ctc.Id}.pdf");
        }
    }
}
