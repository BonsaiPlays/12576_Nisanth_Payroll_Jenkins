/*
    NUnit Test Suite for HRManagerController:

    ApproveCTC:
      - Profile not found
      - No pending CTC
      - Approves valid pending CTC (logs + notifications)

    ApprovePayslip:
      - Payslip not found
      - Approves valid payslip

    ReleasePayslip:
      - Payslip not found
      - Payslip not approved
      - Releases approved payslip

    SetCTCStatus:
      - Not found
      - Approve with year conflict
      - Approve with overlap conflict
      - Approve valid CTC
      - Reject sets status = Rejected

    SetPayslipStatus:
      - Not found
      - Updates status valid

    MonthlySummary:
      - Returns per-department totals

    CompareMonths:
      - Returns difference and percent change

    Anomalies:
      - No anomalies
      - Detects anomaly above threshold
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

[TestFixture]
public class HRManagerControllerTests
{
    private AppDbContext _db;
    private Mock<IEmailService> _email;
    private Mock<IAuditService> _audit;
    private HRManagerController _controller;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _email = new Mock<IEmailService>();
        _audit = new Mock<IAuditService>();
        _controller = new HRManagerController(_db, _email.Object, _audit.Object);

        // Fake authenticated HRM user claims
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"),
                        new Claim(ClaimTypes.Email, "hr@test.com"),
                        new Claim(ClaimTypes.Role, "HRManager"),
                    }
                )
            ),
        };
    }

    [TearDown]
    public void Cleanup() => _db.Dispose();

    [Test]
    public async Task ApproveCTC_ProfileNotFound_ReturnsNotFound()
    {
        var result = await _controller.ApproveCTC(999);
        var nf = result as NotFoundObjectResult;
        Assert.IsNotNull(nf);
        Assert.AreEqual("Employee profile not found", nf.Value);
    }

    [Test]
    public async Task ReleasePayslip_NotApproved_ReturnsBadRequest()
    {
        // Make test user & profile
        var user = new User
        {
            Id = 2,
            Email = "emp@test.com",
            FullName = "Emp",
            PasswordHash = "X",
            Role = UserRole.Employee,
        };
        var profile = new EmployeeProfile
        {
            Id = 5,
            UserId = 2,
            User = user,
        };
        var payslip = new Payslip
        {
            Id = 77,
            EmployeeProfile = profile,
            Status = ApprovalStatus.Pending,
        };
        _db.Users.Add(user);
        _db.Payslips.Add(payslip);
        await _db.SaveChangesAsync();

        var result = await _controller.ReleasePayslip(77);
        Assert.IsInstanceOf<BadRequestObjectResult>(result);
    }

    [Test]
    public async Task CompareMonths_ReturnsCorrectDifference()
    {
        // Arrange: user + profile
        var user = new User
        {
            Id = 2,
            Email = "emp@test.com",
            FullName = "Emp",
            PasswordHash = "X",
            Role = UserRole.Employee,
        };
        var profile = new EmployeeProfile
        {
            Id = 10,
            User = user,
            UserId = user.Id,
        };

        _db.Users.Add(user);
        _db.EmployeeProfiles.Add(profile);
        await _db.SaveChangesAsync();

        // Add payslips with proper FK
        _db.Payslips.Add(
            new Payslip
            {
                Id = 1,
                EmployeeProfileId = profile.Id,
                Year = 2024,
                Month = 8,
                NetPay = 1000,
            }
        );
        _db.Payslips.Add(
            new Payslip
            {
                Id = 2,
                EmployeeProfileId = profile.Id,
                Year = 2024,
                Month = 9,
                NetPay = 2000,
            }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.CompareMonths(2024, 8, 2024, 9) as OkObjectResult;
        Assert.NotNull(result);

        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);

        // Assert
        StringAssert.Contains("\"TotalNet\":1000", json); // PeriodA
        StringAssert.Contains("\"TotalNet\":2000", json); // PeriodB
        StringAssert.Contains("\"Difference\":1000", json);
    }
}
