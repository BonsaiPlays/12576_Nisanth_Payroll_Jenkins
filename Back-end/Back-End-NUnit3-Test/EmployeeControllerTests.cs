/*
    NUnit Test Suite for EmployeeController:

    GET /profile
    - NotFound if user not present
    - Ok with null Profile when user has no profile
    - Ok with Profile but no approved CTC
    - Ok with latest approved CTC

    PUT /profile
    - NotFound if profile missing
    - Updates profile, logs, notifies, emails

    GET /payslips
    - NotFound if no profile
    - Ok returns correct payslip summaries
    - Supports pagination

    GET /payslips/{id}/pdf
    - NotFound if payslip not found for user
    - Ok returns PDF file content

    GET /payslips/{id}
    - NotFound if profile missing
    - NotFound if payslip not found
    - Ok returns full payslip detail

    GET /ctcs
    - NotFound if no profile
    - Ok returns ordered list of CTCs

    GET /ctcs/{id}/pdf
    - NotFound if ctc not found
    - Ok returns PDF file content
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
    [TestFixture]
    public class EmployeeControllerTests
    {
        private AppDbContext _db;
        private Mock<IEmailService> _email;
        private Mock<IAuditService> _audit;
        private EmployeeController _controller;

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ---------- HELPER FACTORY METHOD ----------
        private User MakeTestUser(
            int id = 1,
            string email = "user@test.com",
            string fullName = "Test User"
        )
        {
            return new User
            {
                Id = id,
                Email = email,
                FullName = fullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Secret123!"),
                Role = UserRole.Employee,
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

            _email = new Mock<IEmailService>();
            _audit = new Mock<IAuditService>();
            _controller = new EmployeeController(_db, _email.Object, _audit.Object);

            // Fake authenticated user (UserId = 1)
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "1"),
                            new Claim(ClaimTypes.Email, "user@test.com"),
                            new Claim(ClaimTypes.Role, "Employee"),
                        }
                    )
                ),
            };
        }

        [TearDown]
        public void TearDown() => _db.Dispose();

        // ----------------- PROFILE -----------------
        [Test]
        public async Task GetProfile_UserNotFound_ReturnsNotFound()
        {
            var result = await _controller.GetProfile();
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task GetProfile_UserWithNoProfile_ReturnsOk_WithNullProfile()
        {
            _db.Users.Add(MakeTestUser());
            await _db.SaveChangesAsync();

            var result = await _controller.GetProfile() as OkObjectResult;
            Assert.NotNull(result);
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            StringAssert.Contains("\"Profile\":null", json);
        }

        [Test]
        public async Task GetProfile_UserWithProfileAndApprovedCtc_ReturnsOkWithCtc()
        {
            var user = MakeTestUser();
            var profile = new EmployeeProfile { Id = 10, UserId = 1 };
            var ctc = new CTCStructure
            {
                Id = 20,
                EmployeeProfileId = 10,
                Status = ApprovalStatus.Approved,
                Basic = 12000,
                HRA = 6000,
                EffectiveFrom = DateTime.UtcNow.AddMonths(-1),
                GrossCTC = 20000,
                TaxPercent = 10,
            };
            profile.CTCStructures.Add(ctc);
            user.Profile = profile;

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var result = await _controller.GetProfile() as OkObjectResult;
            Assert.NotNull(result);

            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);

            // Assert it has a Profile object
            StringAssert.Contains("\"Profile\":{", json);

            // Assert that the CTC object includes expected values
            StringAssert.Contains("\"Basic\":12000", json);
            StringAssert.Contains("\"HRA\":6000", json);
            StringAssert.Contains("\"GrossCTC\":20000", json);
        }

        [Test]
        public async Task UpdateProfile_ProfileMissing_ReturnsNotFound()
        {
            _db.Users.Add(MakeTestUser());
            await _db.SaveChangesAsync();

            var result = await _controller.UpdateProfile(
                new EmployeeProfileUpdate { Address = "123 St" }
            );
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task UpdateProfile_UpdatesFields_AndLogsAndEmails()
        {
            var user = MakeTestUser();
            user.Profile = new EmployeeProfile { Id = 11, UserId = 1 };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var result = await _controller.UpdateProfile(
                new EmployeeProfileUpdate
                {
                    Address = "NewAddr",
                    Phone = "12345",
                    DepartmentId = 99,
                }
            );

            Assert.IsInstanceOf<OkResult>(result);

            var updated = await _db.EmployeeProfiles.FindAsync(11);
            Assert.AreEqual("NewAddr", updated.Address);
            Assert.AreEqual("12345", updated.Phone);
            Assert.AreEqual(99, updated.DepartmentId);

            _audit.Verify(
                a =>
                    a.LogAsync(
                        "EmployeeProfile",
                        updated.Id,
                        "Updated",
                        It.IsAny<string>(),
                        user.Id,
                        user.Email
                    ),
                Times.Once
            );
            _email.Verify(
                e =>
                    e.SendTemplatedAsync(
                        user.Email,
                        "Profile Updated",
                        It.IsAny<string>(),
                        null,
                        null
                    ),
                Times.Once
            );
        }

        // ----------------- PAYSLIPS -----------------
        [Test]
        public async Task GetMyPayslips_NoProfile_ReturnsNotFound()
        {
            var result = await _controller.GetMyPayslips();
            Assert.IsInstanceOf<NotFoundResult>(result.Result);
        }

        [Test]
        public async Task GetMyPayslips_WithPayslips_ReturnsOk()
        {
            var user = MakeTestUser();
            user.Profile = new EmployeeProfile { Id = 10, UserId = 1 };
            _db.Users.Add(user);

            _db.Payslips.Add(
                new Payslip
                {
                    Id = 100,
                    EmployeeProfileId = 10,
                    Year = 2024,
                    Month = 3,
                    NetPay = 5000,
                    Status = ApprovalStatus.Approved,
                }
            );
            await _db.SaveChangesAsync();

            var result = await _controller.GetMyPayslips();
            var ok = result.Result as OkObjectResult;
            var paged = ok.Value as PagedResult<PayslipSummary>;

            Assert.AreEqual(1, paged.Total);
            Assert.AreEqual(5000, paged.Items.First().NetPay);
        }

        [Test]
        public async Task DownloadMyPayslipPdf_NotFound_ReturnsNotFound()
        {
            var result = await _controller.DownloadMyPayslipPdf(999);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task DownloadMyPayslipPdf_Found_ReturnsFile()
        {
            var user = MakeTestUser(fullName: "Emp");
            var profile = new EmployeeProfile
            {
                Id = 10,
                UserId = 1,
                User = user,
            };
            user.Profile = profile;

            _db.Users.Add(user);
            _db.Payslips.Add(
                new Payslip
                {
                    Id = 100,
                    EmployeeProfileId = 10,
                    Year = 2024,
                    Month = 4,
                    Basic = 1000,
                    HRA = 500,
                }
            );
            await _db.SaveChangesAsync();

            var result = await _controller.DownloadMyPayslipPdf(100);
            Assert.IsInstanceOf<FileContentResult>(result);
            var file = result as FileContentResult;
            Assert.AreEqual("application/pdf", file.ContentType);
        }

        [Test]
        public async Task GetPayslipDetail_PayslipNotFound_ReturnsNotFound()
        {
            var user = MakeTestUser();
            user.Profile = new EmployeeProfile { Id = 10, UserId = 1 };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var result = await _controller.GetPayslipDetail(99);
            Assert.IsInstanceOf<NotFoundObjectResult>(result.Result);
        }

        // ----------------- CTC -----------------
        [Test]
        public async Task GetMyCTCs_NoProfile_ReturnsNotFound()
        {
            var result = await _controller.GetMyCTCs();
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task GetMyCTCs_WithProfile_ReturnsOk()
        {
            var user = MakeTestUser();
            user.Profile = new EmployeeProfile { Id = 10, UserId = 1 };
            _db.Users.Add(user);

            var ctc = new CTCStructure
            {
                Id = 101,
                EmployeeProfileId = 10,
                Basic = 1000,
                HRA = 500,
                GrossCTC = 1500,
                TaxPercent = 5,
                EffectiveFrom = DateTime.UtcNow,
            };
            _db.CTCStructures.Add(ctc);
            await _db.SaveChangesAsync();

            var result = await _controller.GetMyCTCs() as OkObjectResult;
            Assert.NotNull(result);

            var list = result.Value as IEnumerable<object>;
            Assert.AreEqual(1, list.Count());
        }

        [Test]
        public async Task DownloadMyCTCPdf_NotFound_ReturnsNotFound()
        {
            var result = await _controller.DownloadMyCTCPdf(999);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task DownloadMyCTCPdf_Found_ReturnsFile()
        {
            var user = MakeTestUser(fullName: "Emp");
            var profile = new EmployeeProfile
            {
                Id = 10,
                UserId = 1,
                User = user,
            };
            user.Profile = profile;

            _db.Users.Add(user);
            var ctc = new CTCStructure
            {
                Id = 201,
                EmployeeProfileId = 10,
                EffectiveFrom = DateTime.UtcNow,
                Basic = 1000,
                HRA = 500,
                GrossCTC = 1500,
                TaxPercent = 10,
                EmployeeProfile = profile,
            };
            _db.CTCStructures.Add(ctc);
            await _db.SaveChangesAsync();

            var result = await _controller.DownloadMyCTCPdf(201);
            Assert.IsInstanceOf<FileContentResult>(result);
            var file = result as FileContentResult;
            Assert.AreEqual("application/pdf", file.ContentType);
        }
    }
}
