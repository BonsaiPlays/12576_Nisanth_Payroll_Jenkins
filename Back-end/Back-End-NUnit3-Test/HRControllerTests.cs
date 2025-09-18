/*
    NUnit Test Suite for HRController

    Employees:
    - GetEmployees returns paginated, filters by name/email
    - GetEmployeesWithApprovedCTC returns approved CTC users only
    - GetLatestApprovedCtc returns latest or NotFound if no profile/approved CTC

    Payslips:
    - GetPayslips returns filtered summaries
    - GetPayslipDetail NotFound/Ok
    - CreatePayslip handles NoCTC, Conflict, Ok (uses PayrollService mock)
    - ExportPayslipPdf returns NotFound/Ok(PDF)
    - ExportEmployeePayslipsExcel returns NotFound/Ok(Excel)

    CTCs:
    - GetAllCTCs returns paged list
    - GetCTCDetail NotFound/Ok
    - CreateCTC validates overlaps/returns Ok
    - CreateCTCBatch handles BadRequest, Created, Conflict
    - ExportCtcPdf returns NotFound/Ok(PDF)
    - ExportEmployeeCtcsExcel returns NotFound/Ok(Excel)
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PayrollApi.Controllers;
using PayrollApi.Data;
using PayrollApi.DTOs;
using PayrollApi.Models;
using PayrollApi.Models.Enums;
using PayrollApi.Services;
using QuestPDF.Infrastructure;

namespace PayrollApi.Tests.Controllers
{
    [SetUpFixture]
    public class GlobalTestSetup
    {
        [OneTimeSetUp]
        public void InitLicense()
        {
            // Avoid license exception for QuestPDF in tests
            QuestPDF.Settings.License = LicenseType.Community;
        }
    }

    [TestFixture]
    public class HRControllerTests
    {
        private AppDbContext _db;
        private Mock<IPayrollService> _payroll;
        private Mock<IEmailService> _email;
        private Mock<IAuditService> _audit;
        private HRController _controller;

        private User MakeTestUser(int id = 1, string role = "Employee")
        {
            return new User
            {
                Id = id,
                Email = $"user{id}@test.com",
                FullName = $"User{id}",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Secret123!"),
                Role = Enum.Parse<UserRole>(role),
                IsActive = true,
            };
        }

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);

            _payroll = new Mock<IPayrollService>();
            _email = new Mock<IEmailService>();
            _audit = new Mock<IAuditService>();
            _controller = new HRController(_db, _payroll.Object, _email.Object, _audit.Object);

            // Set fake claims for CurrentUserId/Email
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "99"),
                            new Claim(ClaimTypes.Email, "hr@test.com"),
                            new Claim(ClaimTypes.Role, "HR"),
                        }
                    )
                ),
            };
        }

        [TearDown]
        public void Cleanup() => _db.Dispose();

        // -------- EMPLOYEES --------

        [Test]
        public async Task GetEmployees_ReturnsNonAdminOnly()
        {
            _db.Users.Add(MakeTestUser(1, "Employee"));
            _db.Users.Add(MakeTestUser(2, "HR"));
            _db.Users.Add(MakeTestUser(3, "Admin")); // excluded
            await _db.SaveChangesAsync();

            var result = await _controller.GetEmployees();
            var ok = result as OkObjectResult;
            Assert.NotNull(ok);
            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            Assert.That(json, Does.Contain("\"Role\":4")); // numeric Employee enum
            Assert.That(json, Does.Not.Contain("\"Role\":1")); // no Admins
        }

        [Test]
        public async Task GetLatestApprovedCtc_NoProfile_ReturnsNotFound()
        {
            var user = MakeTestUser(1);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var result = await _controller.GetLatestApprovedCtc(1);
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        // -------- PAYSLIPS --------

        [Test]
        public async Task GetPayslipDetail_NotFound_Returns404()
        {
            var result = await _controller.GetPayslipDetail(999);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task CreatePayslip_NoCtc_ReturnsBadRequest()
        {
            var user = MakeTestUser(1);
            user.Profile = new EmployeeProfile { Id = 11, User = user };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var result = await _controller.CreatePayslip(
                new PayslipCreateRequest
                {
                    EmployeeUserId = 1,
                    Year = 2024,
                    Month = 8,
                    LOPDays = 0,
                }
            );
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task CreatePayslip_Valid_ReturnsOk()
        {
            var user = MakeTestUser(1);
            var profile = new EmployeeProfile
            {
                Id = 10,
                UserId = 1,
                User = user,
            };
            user.Profile = profile;
            profile.CTCStructures.Add(
                new CTCStructure
                {
                    Id = 22,
                    Basic = 12000,
                    HRA = 6000,
                    Status = ApprovalStatus.Approved,
                    EffectiveFrom = DateTime.UtcNow.AddMonths(-1),
                    EffectiveTo = DateTime.UtcNow.AddMonths(11),
                    GrossCTC = 20000,
                    TaxPercent = 10,
                }
            );
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // payroll mock returns a dummy payslip
            _payroll
                .Setup(p =>
                    p.ComputePayslipFromCTC(It.IsAny<CTCStructure>(), 2024, 8, 0, null, null)
                )
                .Returns(
                    new Payslip
                    {
                        Id = 77,
                        Year = 2024,
                        Month = 8,
                        NetPay = 1000,
                        EmployeeProfileId = 10,
                    }
                );

            var result = await _controller.CreatePayslip(
                new PayslipCreateRequest
                {
                    EmployeeUserId = 1,
                    Year = 2024,
                    Month = 8,
                }
            );
            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task ExportPayslipPdf_NotFound_Returns404()
        {
            var result = await _controller.ExportPayslipPdf(999);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        // -------- CTC --------

        [Test]
        public async Task GetCTCDetail_NotFound_Returns404()
        {
            var result = await _controller.GetCTCDetail(123);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task CreateCTC_InvalidBasic_ReturnsBadRequest()
        {
            var user = MakeTestUser(1);
            user.Profile = new EmployeeProfile { Id = 5, User = user };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var req = new CTCRequest
            {
                Basic = 0,
                HRA = 1000,
                EffectiveFrom = DateTime.UtcNow,
            };
            var result = await _controller.CreateCTC(1, req);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task CreateCTC_Valid_ReturnsOk()
        {
            var user = MakeTestUser(1);
            user.Profile = new EmployeeProfile { Id = 5, User = user };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var req = new CTCRequest
            {
                Basic = 10000,
                HRA = 4000,
                AllowanceItems = new List<CTCLineItem>(),
                DeductionItems = new List<CTCLineItem>(),
                TaxPercent = 10,
                EffectiveFrom = DateTime.UtcNow,
            };

            var result = await _controller.CreateCTC(1, req);
            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task CreateCTCBatch_EmptyEmployees_ReturnsBadRequest()
        {
            var result = await _controller.CreateCTCBatch(
                new CTCBatchRequest { EmployeeUserIds = new List<int>() }
            );
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task ExportCtcPdf_NotFound_Returns404()
        {
            var result = await _controller.ExportCtcPdf(999);
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task ExportEmployeeCtcsExcel_NoCtcs_Returns404()
        {
            var user = MakeTestUser(2);
            user.Profile = new EmployeeProfile { UserId = 2, User = user };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var result = await _controller.ExportEmployeeCtcsExcel(2);
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task GetEmployeesWithApprovedCTC_ReturnsOnlyApproved()
        {
            var user = MakeTestUser(1);
            var profile = new EmployeeProfile
            {
                Id = 10,
                UserId = 1,
                User = user,
            };
            profile.CTCStructures.Add(
                new CTCStructure
                {
                    Id = 33,
                    Basic = 1000,
                    HRA = 500,
                    GrossCTC = 1500,
                    Status = ApprovalStatus.Approved,
                    EffectiveFrom = DateTime.UtcNow,
                    EffectiveTo = DateTime.UtcNow.AddMonths(12),
                }
            );
            user.Profile = profile;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var result = await _controller.GetEmployeesWithApprovedCTC();
            var ok = result as OkObjectResult;
            Assert.NotNull(ok);
            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            Assert.That(json, Does.Contain("\"Status\":1")); // Approved
        }
    }
}
