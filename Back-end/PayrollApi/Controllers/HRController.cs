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
    [Route("api/hr")]
    [Route("api/hr-manager")]
    [Authorize(Roles = "HR,HRManager,Admin")]
    public class HRController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IPayrollService _payroll;
        private readonly IEmailService _email;
        private readonly IAuditService _audit;

        public HRController(
            AppDbContext db,
            IPayrollService payroll,
            IEmailService email,
            IAuditService audit
        )
        {
            _db = db;
            _payroll = payroll;
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
        /// Retrieves a paginated list of non-admin employees with optional search and department filtering.
        /// </summary>
        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] int? departmentId = null
        )
        {
            var q = _db.Users.Include(u => u.Profile).ThenInclude(p => p.Department).AsQueryable();

            // Exclude Admins
            q = q.Where(u => u.Role != UserRole.Admin);

            // Case-insensitive search by name/email
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowered = search.ToLower();
                q = q.Where(u =>
                    u.FullName.ToLower().Contains(lowered) || u.Email.ToLower().Contains(lowered)
                );
            }

            // Optional filter by department
            if (departmentId.HasValue)
            {
                q = q.Where(u => u.Profile != null && u.Profile.DepartmentId == departmentId.Value);
            }

            // Fetch paged result
            var (total, items) = await q.OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.Role,
                    Department = u.Profile != null && u.Profile.Department != null
                        ? u.Profile.Department.Name
                        : null,
                })
                .ToPagedAsync(page, pageSize);

            return Ok(
                new PagedResult<object>
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    Items = items,
                }
            );
        }

        /// <summary>
        /// Retrieves a paginated list of payslips with optional filtering by department, year, month, and search term.
        /// </summary>
        [HttpGet("payslips")]
        public async Task<IActionResult> GetPayslips([FromQuery] PayslipFilter filter)
        {
            var q = _db
                .Payslips.Include(p => p.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .Include(p => p.EmployeeProfile.Department)
                .Include(p => p.AllowanceItems)
                .Include(p => p.DeductionItems)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            if (filter.EmployeeUserId.HasValue)
                q = q.Where(p => p.EmployeeProfile.UserId == filter.EmployeeUserId); // <-- new filter
            if (filter.DepartmentId.HasValue)
                q = q.Where(p => p.EmployeeProfile.DepartmentId == filter.DepartmentId);
            if (filter.Year.HasValue)
                q = q.Where(p => p.Year == filter.Year);
            if (filter.Month.HasValue)
                q = q.Where(p => p.Month == filter.Month);
            if (!string.IsNullOrWhiteSpace(filter.Search))
                q = q.Where(p =>
                    p.EmployeeProfile.User.FullName.Contains(filter.Search)
                    || p.EmployeeProfile.User.Email.Contains(filter.Search)
                );

            var (total, items) = await q.OrderByDescending(p => p.CreatedAt)
                .ToPagedAsync(filter.Page, filter.PageSize);

            var shaped = items.Select(s => new
            {
                s.Id,
                s.Year,
                s.Month,
                s.NetPay,
                s.IsReleased,
                s.Status,
                Employee = new
                {
                    s.EmployeeProfile.UserId,
                    s.EmployeeProfile.User.FullName,
                    s.EmployeeProfile.User.Email,
                },
                Department = s.EmployeeProfile.Department?.Name,
                Allowances = s.AllowanceItems.Select(a => new { a.Label, a.Amount }),
                Deductions = s.DeductionItems.Select(d => new { d.Label, d.Amount }),
            });

            return Ok(
                new DTOs.PagedResult<object>
                {
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    Total = total,
                    Items = shaped,
                }
            );
        }

        /// <summary>
        /// Retrieves a paginated list of all CTC structures with employee and department details.
        /// </summary>
        [HttpGet("ctcs")]
        public async Task<IActionResult> GetAllCTCs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] int? employeeId = null
        )
        {
            try
            {
                var q = _db
                    .CTCStructures.Include(c => c.EmployeeProfile)
                    .ThenInclude(ep => ep.User)
                    .Include(c => c.EmployeeProfile.Department)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsQueryable();

                if (employeeId.HasValue)
                {
                    q = q.Where(c => c.EmployeeProfile.UserId == employeeId.Value);
                }

                var total = await q.CountAsync();

                var items = await q.Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        Type = "CTC",
                        c.Id,
                        c.EffectiveFrom,
                        c.GrossCTC,
                        c.Status,
                        Employee = c.EmployeeProfile != null && c.EmployeeProfile.User != null
                            ? new
                            {
                                c.EmployeeProfile.UserId,
                                c.EmployeeProfile.User.FullName,
                                c.EmployeeProfile.User.Email,
                            }
                            : null,
                        Department = c.EmployeeProfile == null
                            ? null
                            : (
                                c.EmployeeProfile.Department == null
                                    ? null
                                    : c.EmployeeProfile.Department.Name
                            ),
                    })
                    .ToListAsync();

                return Ok(
                    new
                    {
                        Page = page,
                        PageSize = pageSize,
                        Total = total,
                        Items = items,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        /// <summary>
        /// Retrieves detailed information for a specific payslip including employee, allowances, and deductions.
        /// </summary>
        [HttpGet("payslips/{id}/detail")]
        public async Task<IActionResult> GetPayslipDetail(int id)
        {
            var slip = await _db
                .Payslips.Include(p => p.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .Include(p => p.AllowanceItems)
                .Include(p => p.DeductionItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (slip == null)
                return NotFound();

            return Ok(
                new
                {
                    id = slip.Id,
                    year = slip.Year,
                    month = slip.Month,
                    netPay = slip.NetPay,
                    taxDeducted = slip.TaxDeducted,
                    status = slip.Status,
                    isReleased = slip.IsReleased,
                    employee = new
                    {
                        userId = slip.EmployeeProfile.User.Id,
                        fullName = slip.EmployeeProfile.User.FullName,
                        email = slip.EmployeeProfile.User.Email,
                    },
                    allowances = slip.AllowanceItems.Select(a => new
                    {
                        label = a.Label,
                        amount = a.Amount,
                    }),
                    deductions = slip.DeductionItems.Select(d => new
                    {
                        label = d.Label,
                        amount = d.Amount,
                    }),
                    type = "Payslip",
                }
            );
        }

        /// <summary>
        /// Retrieves detailed information for a specific CTC structure including employee, allowances, and deductions.
        /// </summary>
        [HttpGet("ctcs/{id}/detail")]
        public async Task<IActionResult> GetCTCDetail(int id)
        {
            var ctc = await _db
                .CTCStructures.Include(c => c.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .Include(c => c.Allowances)
                .Include(c => c.Deductions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (ctc == null)
                return NotFound();

            return Ok(
                new
                {
                    id = ctc.Id,
                    basic = ctc.Basic,
                    hra = ctc.HRA,
                    grossCTC = ctc.GrossCTC,
                    taxPercent = ctc.TaxPercent,
                    status = ctc.Status,
                    effectiveFrom = ctc.EffectiveFrom,
                    employee = new
                    {
                        userId = ctc.EmployeeProfile.User.Id,
                        fullName = ctc.EmployeeProfile.User.FullName,
                        email = ctc.EmployeeProfile.User.Email,
                    },
                    allowances = ctc.Allowances.Select(a => new
                    {
                        label = a.Label,
                        amount = a.Amount,
                    }),
                    deductions = ctc.Deductions.Select(d => new
                    {
                        label = d.Label,
                        amount = d.Amount,
                    }),
                    type = "CTC",
                }
            );
        }

        /// <summary>
        /// Creates a new CTC structure for the specified employee and notifies them via email and in-app notification.
        /// </summary>
        [HttpPost("ctc/{employeeUserId}")]
        public async Task<IActionResult> CreateCTC(int employeeUserId, CTCRequest req)
        {
            var user = await _db
                .Users.Include(u => u.Profile)
                .ThenInclude(p => p.Department)
                .Include(u => u.Profile!.CTCStructures)
                .FirstOrDefaultAsync(u => u.Id == employeeUserId);

            if (user?.Profile == null)
                return NotFound("Employee profile not found");

            if (req.Basic <= 0)
                return BadRequest(new { error = "Basic pay must be greater than 0" });
            if (req.HRA < 0)
                return BadRequest(new { error = "HRA cannot be negative" });
            if (req.HRA > req.Basic * 0.5M)
                return BadRequest(new { error = "HRA cannot exceed 50% of Basic" });
            if (req.AllowanceItems.GroupBy(a => a.Label.ToLower()).Any(g => g.Count() > 1))
                return BadRequest(new { error = "Duplicate allowance labels not allowed" });
            if (req.DeductionItems.GroupBy(d => d.Label.ToLower()).Any(g => g.Count() > 1))
                return BadRequest(new { error = "Duplicate deduction labels not allowed" });
            if (req.EffectiveFrom == default)
                return BadRequest(new { error = "EffectiveFrom is required" });

            // Each new CTC valid for 1 year exactly
            var effectiveFrom = req.EffectiveFrom;
            var effectiveTo = req.EffectiveFrom.AddYears(1);

            var gross = req.Basic + req.HRA + req.AllowanceItems.Sum(a => a.Amount);

            var ctc = new CTCStructure
            {
                EmployeeProfileId = user.Profile.Id,
                Basic = req.Basic,
                HRA = req.HRA,
                TaxPercent = Math.Round(req.TaxPercent, 2, MidpointRounding.AwayFromZero),
                GrossCTC = gross,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo, // <---- auto set one year validity
                Status = ApprovalStatus.Pending,
                CreatedByUserId = CurrentUserId,
                Allowances = req
                    .AllowanceItems.Select(a => new CTCAllowance
                    {
                        Label = a.Label,
                        Amount = a.Amount,
                    })
                    .ToList(),
                Deductions = req
                    .DeductionItems.Select(d => new CTCDeduction
                    {
                        Label = d.Label,
                        Amount = d.Amount,
                    })
                    .ToList(),
            };

            _db.CTCStructures.Add(ctc);

            // Logging
            await _audit.LogAsync(
                "CTCStructure",
                ctc.Id,
                "Created",
                $"CTC created for {user.FullName} ({user.Email}), effective {ctc.EffectiveFrom:yyyy-MM-dd}",
                CurrentUserId,
                CurrentUserEmail
            );

            await _db.SaveChangesAsync();

            // Notify all HR Managers
            var hrManagers = await _db.Users.Where(u => u.Role == UserRole.HRManager).ToListAsync();

            foreach (var hrm in hrManagers)
            {
                _db.Notifications.Add(
                    new Notification
                    {
                        UserId = hrm.Id,
                        Subject = $"CTC Requires Approval",
                        Message = $"A new CTC for {user.FullName} is pending approval.",
                    }
                );

                await _email.SendTemplatedAsync(
                    hrm.Email,
                    "CTC Requires Approval",
                    $"Hello {hrm.FullName},<br/>A CTC for {user.FullName} effective {ctc.EffectiveFrom:dd-MMM-yyyy} requires your approval."
                );
            }

            await _db.SaveChangesAsync();

            return Ok(new { Message = "CTC created and awaiting approval" });
        }

        /// <summary>
        /// Creates new CTCs for multiple employees in one request.
        /// Returns per-employee success or conflict results for display.
        /// </summary>
        [HttpPost("ctc/batch")]
        public async Task<IActionResult> CreateCTCBatch([FromBody] CTCBatchRequest req)
        {
            if (req.EmployeeUserIds == null || !req.EmployeeUserIds.Any())
                return BadRequest(new { error = "At least one employee required" });

            var results = new List<CTCBatchResult>();

            foreach (var empId in req.EmployeeUserIds.Distinct())
            {
                var user = await _db
                    .Users.Include(u => u.Profile)
                    .ThenInclude(p => p.Department)
                    .Include(u => u.Profile!.CTCStructures)
                    .FirstOrDefaultAsync(u => u.Id == empId);

                if (user?.Profile == null)
                {
                    results.Add(
                        new CTCBatchResult
                        {
                            EmployeeId = empId,
                            Employee = $"#{empId}",
                            Email = "-",
                            Status = "Error",
                            Message = "Employee profile not found",
                        }
                    );
                    continue;
                }

                try
                {
                    if (req.Basic <= 0)
                        throw new Exception("Basic must be greater than 0");
                    if (req.HRA < 0)
                        throw new Exception("HRA cannot be negative");
                    if (req.HRA > req.Basic * 0.5M)
                        throw new Exception("HRA cannot exceed 50% of Basic");
                    if (req.Basic >= 20000 && (req.TaxPercent <= 0 || req.TaxPercent > 50))
                        throw new Exception("Tax percent must be 1–50 when Basic >= 20000");

                    if (req.AllowanceItems.GroupBy(a => a.Label.ToLower()).Any(g => g.Count() > 1))
                        throw new Exception("Duplicate allowance labels not allowed");
                    if (req.DeductionItems.GroupBy(d => d.Label.ToLower()).Any(g => g.Count() > 1))
                        throw new Exception("Duplicate deduction labels not allowed");

                    if (req.EffectiveFrom == default)
                        throw new Exception("EffectiveFrom is required");

                    var selectedYear = req.EffectiveFrom.Year;
                    bool existsInYear = user.Profile.CTCStructures.Any(c =>
                        (c.Status == ApprovalStatus.Pending || c.Status == ApprovalStatus.Approved)
                        && c.EffectiveFrom.Year == selectedYear
                    );

                    if (existsInYear)
                        throw new Exception(
                            $"Conflict: employee already has CTC in {selectedYear}"
                        );

                    var effectiveTo = req.EffectiveFrom.AddYears(1);
                    bool overlap = user.Profile.CTCStructures.Any(c =>
                        c.Status == ApprovalStatus.Approved
                        && c.EffectiveFrom < effectiveTo
                        && c.EffectiveTo > req.EffectiveFrom
                    );
                    if (overlap)
                        throw new Exception("Employee already has active overlapping CTC");

                    // BUILD NEW CTC
                    var gross = req.Basic + req.HRA + req.AllowanceItems.Sum(a => a.Amount);

                    var ctc = new CTCStructure
                    {
                        EmployeeProfileId = user.Profile.Id,
                        Basic = req.Basic,
                        HRA = req.HRA,
                        TaxPercent = Math.Round(req.TaxPercent, 2, MidpointRounding.AwayFromZero),
                        GrossCTC = gross,
                        EffectiveFrom = req.EffectiveFrom,
                        EffectiveTo = effectiveTo,
                        Status = ApprovalStatus.Pending,
                        CreatedByUserId = CurrentUserId,
                        Allowances = req
                            .AllowanceItems.Select(a => new CTCAllowance
                            {
                                Label = a.Label,
                                Amount = a.Amount,
                            })
                            .ToList(),
                        Deductions = req
                            .DeductionItems.Select(d => new CTCDeduction
                            {
                                Label = d.Label,
                                Amount = d.Amount,
                            })
                            .ToList(),
                    };

                    _db.CTCStructures.Add(ctc);

                    results.Add(
                        new CTCBatchResult
                        {
                            EmployeeId = empId,
                            Employee = user.FullName,
                            Email = user.Email,
                            Status = "Created",
                            Message = "CTC created & pending approval",
                        }
                    );

                    // TODO: add notifications/email per HR Manager (optional)
                }
                catch (Exception ex)
                {
                    results.Add(
                        new CTCBatchResult
                        {
                            EmployeeId = empId,
                            Employee = user.FullName,
                            Email = user.Email,
                            Status = "Conflict",
                            Message = ex.Message,
                        }
                    );
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { Results = results });
        }

        /// <summary>
        /// Creates a new payslip for the specified employee based on their latest approved CTC and notifies them via email and in-app notification.
        /// </summary>
        [HttpPost("payslips")]
        public async Task<IActionResult> CreatePayslip(PayslipCreateRequest req)
        {
            var user = await _db
                .Users.Include(u => u.Profile)
                .ThenInclude(p => p.CTCStructures)
                .FirstOrDefaultAsync(u => u.Id == req.EmployeeUserId);

            if (user?.Profile?.CTCStructures == null)
                return BadRequest("CTC not set");

            var latestCtc = user
                .Profile.CTCStructures.Where(c => c.Status == ApprovalStatus.Approved)
                .OrderByDescending(c => c.EffectiveFrom)
                .FirstOrDefault();

            if (latestCtc == null)
                return BadRequest("No approved CTC found");

            var slip = _payroll.ComputePayslipFromCTC(
                latestCtc,
                req.Year,
                req.Month,
                req.LOPDays,
                req.OverridesAllowances,
                req.OverridesDeductions
            );
            slip.EmployeeProfileId = user.Profile.Id;
            slip.CreatedByUserId = CurrentUserId;

            _db.Payslips.Add(slip);
            await _db.SaveChangesAsync();

            // Logging
            await _audit.LogAsync(
                "Payslip",
                slip.Id,
                "Created",
                $"Payslip {req.Month}/{req.Year} created for {user.FullName} ({user.Email})",
                CurrentUserId,
                CurrentUserEmail
            );

            // Notification
            var hrManagers = await _db.Users.Where(u => u.Role == UserRole.HRManager).ToListAsync();
            foreach (var hrm in hrManagers)
            {
                _db.Notifications.Add(
                    new Notification
                    {
                        UserId = hrm.Id,
                        Subject = "Payslip Requires Approval",
                        Message =
                            $"Payslip for {user.FullName} ({req.Month}/{req.Year}) awaits approval.",
                    }
                );

                //Mail
                await _email.SendTemplatedAsync(
                    hrm.Email,
                    "Payslip Requires Approval",
                    $"Hello {hrm.FullName},<br/>A payslip for {user.FullName} ({req.Month}/{req.Year}) is awaiting your approval."
                );
            }
            await _db.SaveChangesAsync();

            return Ok(
                new
                {
                    slip.Id,
                    slip.Year,
                    slip.Month,
                    slip.NetPay,
                    slip.Status,
                    Allowances = slip.AllowanceItems.Select(a => new { a.Label, a.Amount }),
                    Deductions = slip.DeductionItems.Select(d => new { d.Label, d.Amount }),
                }
            );
        }

        /// <summary>
        /// Exports a payslip as a PDF file for download.
        /// </summary>
        [HttpGet("exports/payslips/pdf/{payslipId}")]
        public async Task<IActionResult> ExportPayslipPdf(int payslipId)
        {
            var slip = await _db
                .Payslips.Include(p => p.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .FirstOrDefaultAsync(p => p.Id == payslipId);

            if (slip == null)
                return NotFound();

            var pdf = PdfExporter.GeneratePayslipPdf(slip, slip.EmployeeProfile.User.FullName);

            var empName = slip.EmployeeProfile.User.FullName.Replace(" ", "_");
            var filename = $"{empName}_Payslip_{slip.Year}-{slip.Month:00}.pdf";

            return File(pdf, "application/pdf", filename);
        }

        /// <summary>
        /// Exports a CTC structure as a PDF file for download.
        /// </summary>
        [HttpGet("ctcs/{ctcId}/pdf")]
        public async Task<IActionResult> ExportCtcPdf(int ctcId)
        {
            var ctc = await _db
                .CTCStructures.Include(c => c.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .Include(c => c.Allowances)
                .Include(c => c.Deductions)
                .FirstOrDefaultAsync(c => c.Id == ctcId);

            if (ctc == null)
                return NotFound("CTC not found");

            var pdf = PdfExporter.GenerateCtcPdf(ctc, ctc.EmployeeProfile.User.FullName);

            var empName = ctc.EmployeeProfile.User.FullName.Replace(" ", "_");
            var filename = $"{empName}_CTC_{ctc.EffectiveFrom:yyyy-MM-dd}.pdf";

            return File(pdf, "application/pdf", filename);
        }

        /// <summary>
        /// Exports all payslips for a specific employee as an Excel file for download.
        /// </summary>
        [HttpGet("exports/payslips/excel")]
        public async Task<IActionResult> ExportEmployeePayslipsExcel([FromQuery] int employeeId)
        {
            var slips = await _db
                .Payslips.Include(p => p.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .Include(p => p.AllowanceItems)
                .Include(p => p.DeductionItems)
                .Where(p => p.EmployeeProfile.UserId == employeeId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            if (!slips.Any())
                return NotFound("No payslips found for this employee");

            var bytes = ExcelExporter.ExportPayslips(slips);

            var empName = slips.First().EmployeeProfile.User.FullName.Replace(" ", "_");
            var filename = $"{empName}_Payslips.xlsx";

            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{filename}\"";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        /// <summary>
        /// Exports all CTC structures for a specific employee as an Excel file for download.
        /// </summary>
        [HttpGet("exports/ctcs/excel")]
        public async Task<IActionResult> ExportEmployeeCtcsExcel([FromQuery] int employeeId)
        {
            var ctcs = await _db
                .CTCStructures.Include(c => c.EmployeeProfile)
                .ThenInclude(ep => ep.User)
                .Include(c => c.Allowances)
                .Include(c => c.Deductions)
                .Where(c => c.EmployeeProfile.UserId == employeeId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            if (!ctcs.Any())
                return NotFound("No CTCs found for this employee");

            var bytes = ExcelExporter.ExportCTCs(ctcs);

            var empName = ctcs.First().EmployeeProfile.User.FullName.Replace(" ", "_");
            var filename = $"{empName}_CTCs.xlsx";

            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{filename}\"";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        /// <summary>
        /// Retrieves a list of employees who have at least one approved CTC structure, including their latest approved CTC details.
        /// </summary>
        [HttpGet("employees-with-ctc")]
        public async Task<IActionResult> GetEmployeesWithApprovedCTC()
        {
            var emps = await _db
                .Users.Include(u => u.Profile)
                .ThenInclude(p => p.Department)
                .Include(u => u.Profile!.CTCStructures)
                .ThenInclude(c => c.Allowances)
                .Include(u => u.Profile!.CTCStructures)
                .ThenInclude(c => c.Deductions)
                .Where(u =>
                    u.Profile != null
                    && u.Profile.CTCStructures.Any(c => c.Status == ApprovalStatus.Approved)
                )
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    Department = u.Profile!.Department != null ? u.Profile.Department.Name : null,
                    LatestCtc = u.Profile!.CTCStructures.Where(c =>
                            c.Status == ApprovalStatus.Approved
                        )
                        .OrderByDescending(c => c.EffectiveFrom)
                        .Select(c => new
                        {
                            c.Id,
                            c.Basic,
                            c.HRA,
                            c.GrossCTC,
                            c.TaxPercent,
                            c.Status,
                            c.EffectiveFrom,
                            Allowances = c.Allowances.Select(a => new { a.Label, a.Amount }),
                            Deductions = c.Deductions.Select(d => new { d.Label, d.Amount }),
                        })
                        .FirstOrDefault(),
                })
                .ToListAsync();

            return Ok(emps);
        }

        /// <summary>
        /// Retrieves the latest approved CTC structure for the specified employee.
        /// </summary>
        [HttpGet("employees/{userId}/latest-ctc")]
        public async Task<IActionResult> GetLatestApprovedCtc(int userId)
        {
            var profile = await _db
                .EmployeeProfiles.Include(p => p.CTCStructures)
                .ThenInclude(c => c.Allowances)
                .Include(p => p.CTCStructures)
                .ThenInclude(c => c.Deductions)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return NotFound("Employee profile not found");

            var ctc = profile
                .CTCStructures.Where(c => c.Status == ApprovalStatus.Approved)
                .OrderByDescending(c => c.EffectiveFrom)
                .FirstOrDefault();

            if (ctc == null)
                return NotFound("No approved CTC found");

            return Ok(
                new
                {
                    ctc.Id,
                    ctc.Basic,
                    ctc.HRA,
                    ctc.GrossCTC,
                    ctc.TaxPercent,
                    ctc.EffectiveFrom,
                    Allowances = ctc.Allowances.Select(a => new { a.Label, a.Amount }),
                    Deductions = ctc.Deductions.Select(d => new { d.Label, d.Amount }),
                }
            );
        }
    }
}
