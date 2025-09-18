/*
    NUnit Test Suite for AuditController

    GetLogs:
    - Returns only "User/EmployeeProfile" when Admin only
    - Returns only "Payslip/CTCStructure" when HRManager only
    - Returns all logs when both roles
    - EntityType filter applies

    ExportExcel:
    - Returns BadRequest if no logs in range
    - Returns FileContentResult with Admin prefix when Admin
    - Returns FileContentResult with HR prefix when HRManager
*/

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PayrollApi.Controllers;
using PayrollApi.Data;
using PayrollApi.Models;

namespace PayrollApi.Tests.Controllers
{
    [TestFixture]
    public class AuditControllerTests
    {
        private AppDbContext _db;
        private AuditController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new AppDbContext(options);
            _controller = new AuditController(_db);
        }

        [TearDown]
        public void Cleanup() => _db.Dispose();

        private void SetRole(params string[] roles)
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)))
                    ),
                },
            };
        }

        private AuditLog MakeLog(string entity, string action)
        {
            return new AuditLog
            {
                EntityType = entity,
                Action = action,
                PerformedAt = DateTime.UtcNow,
                PerformedBy = "tester",
                PerformedById = 42,
                Details = "UnitTest",
            };
        }

        // -------- GetLogs --------

        [Test]
        public async Task GetLogs_AdminOnly_FiltersCorrectly()
        {
            _db.AuditLogs.Add(MakeLog("User", "Created"));
            _db.AuditLogs.Add(MakeLog("Payslip", "Created"));
            await _db.SaveChangesAsync();

            SetRole("Admin");

            var result = await _controller.GetLogs(
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1)
            );
            var ok = result as OkObjectResult;
            Assert.NotNull(ok);

            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            Assert.That(json, Does.Contain("User"));
            Assert.That(json, Does.Not.Contain("Payslip"));
        }

        [Test]
        public async Task GetLogs_HRManagerOnly_FiltersCorrectly()
        {
            _db.AuditLogs.Add(MakeLog("User", "Created"));
            _db.AuditLogs.Add(MakeLog("CTCStructure", "Approved"));
            await _db.SaveChangesAsync();

            SetRole("HRManager");

            var result = await _controller.GetLogs(
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1)
            );
            var ok = result as OkObjectResult;
            Assert.NotNull(ok);

            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            Assert.That(json, Does.Contain("CTCStructure"));
            Assert.That(json, Does.Not.Contain("User"));
        }

        [Test]
        public async Task GetLogs_BothRoles_ShowsAll()
        {
            _db.AuditLogs.Add(MakeLog("User", "Created"));
            _db.AuditLogs.Add(MakeLog("Payslip", "Approved"));
            await _db.SaveChangesAsync();

            SetRole("Admin", "HRManager");

            var result = await _controller.GetLogs(
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1)
            );
            var ok = result as OkObjectResult;
            Assert.NotNull(ok);

            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            Assert.That(json, Does.Contain("User"));
            Assert.That(json, Does.Contain("Payslip"));
        }

        [Test]
        public async Task GetLogs_WithEntityFilter_FiltersByType()
        {
            _db.AuditLogs.Add(MakeLog("User", "Login"));
            _db.AuditLogs.Add(MakeLog("EmployeeProfile", "Updated"));
            await _db.SaveChangesAsync();

            SetRole("Admin");
            var result = await _controller.GetLogs(
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                "EmployeeProfile"
            );
            var ok = result as OkObjectResult;
            Assert.NotNull(ok);

            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            Assert.That(json, Does.Contain("EmployeeProfile"));
            Assert.That(json, Does.Not.Contain("User"));
        }

        // -------- ExportExcel --------

        [Test]
        public async Task ExportExcel_NoLogs_ReturnsBadRequest()
        {
            SetRole("Admin");
            var res = await _controller.ExportExcel(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
            Assert.IsInstanceOf<BadRequestObjectResult>(res);
        }

        [Test]
        public async Task ExportExcel_WithLogs_ReturnsFile_Admin()
        {
            _db.AuditLogs.Add(MakeLog("User", "Created"));
            await _db.SaveChangesAsync();

            SetRole("Admin");
            var res = await _controller.ExportExcel(
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1)
            );
            Assert.IsInstanceOf<FileContentResult>(res);

            var file = res as FileContentResult;
            Assert.That(
                file.ContentType,
                Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            );
            StringAssert.StartsWith("AdminAudit", file.FileDownloadName);
        }

        [Test]
        public async Task ExportExcel_WithLogs_ReturnsFile_HRManager()
        {
            _db.AuditLogs.Add(MakeLog("Payslip", "Approved"));
            await _db.SaveChangesAsync();

            SetRole("HRManager");
            var res = await _controller.ExportExcel(
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1)
            );
            Assert.IsInstanceOf<FileContentResult>(res);

            var file = res as FileContentResult;
            Assert.That(
                file.ContentType,
                Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            );
            StringAssert.StartsWith("HRAudit", file.FileDownloadName);
        }
    }
}
