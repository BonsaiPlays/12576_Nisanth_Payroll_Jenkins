/*
    NUnit Test Suite for AuthController

    Login Endpoint:
    - InvalidEmail_ReturnsUnauthorized
    - InvalidPassword_ReturnsUnauthorized
    - ValidCredentials_ReturnsTokenAndLogsIn (verifies JWT, email notification, audit log)

    ForgotPasswordRequest Endpoint:
    - EmptyEmail_ReturnsBadRequest
    - NonExistingEmail_ReturnsOkWithoutAuditOrEmail
    - ValidEmail_CreatesAuditAndSendsEmails (verifies audit + admin/user email notifications)

    ChangePassword Endpoint:
    - UserNotFound_ReturnsNotFound
    - WrongCurrentPassword_ReturnsBadRequest
    - CorrectPassword_ChangesHash_AndLogsAndEmails (verifies password re‑hash, audit, notification email)

    Setup/TearDown:
    - Each test uses isolated InMemoryDatabase
    - DbContext disposed properly after each run
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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

namespace PayrollApi.Tests.Controllers
{
    [TestFixture]
    public class AuthControllerTests
    {
        private AppDbContext _dbContext;
        private Mock<IJwtService> _jwtMock;
        private Mock<IEmailService> _emailMock;
        private Mock<IAuditService> _auditMock;
        private AuthController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Always new DB
                .Options;

            _dbContext = new AppDbContext(options);

            _jwtMock = new Mock<IJwtService>();
            _emailMock = new Mock<IEmailService>();
            _auditMock = new Mock<IAuditService>();

            _controller = new AuthController(
                _dbContext,
                _jwtMock.Object,
                _emailMock.Object,
                _auditMock.Object
            );
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
        }

        // ------------------ LOGIN TESTS ------------------ //
        [Test]
        public async Task Login_InvalidEmail_ReturnsUnauthorized()
        {
            var result = await _controller.Login(
                new LoginRequest { Email = "doesnotexist@test.com", Password = "wrong" }
            );

            Assert.IsInstanceOf<UnauthorizedObjectResult>(result.Result);
        }

        [Test]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var user = new User
            {
                Email = "test@test.com",
                FullName = "Test User",
                Role = UserRole.Employee,
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var result = await _controller.Login(
                new LoginRequest { Email = "test@test.com", Password = "WrongPassword" }
            );

            Assert.IsInstanceOf<UnauthorizedObjectResult>(result.Result);
        }

        [Test]
        public async Task Login_ValidCredentials_ReturnsTokenAndLogsIn()
        {
            // Arrange
            var user = new User
            {
                Email = "test@test.com",
                FullName = "Test User",
                Role = UserRole.Employee,
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            _jwtMock
                .Setup(j => j.Generate(user))
                .Returns(("TOKEN123", DateTime.UtcNow.AddHours(1)));

            // Act
            var result = await _controller.Login(
                new LoginRequest { Email = "test@test.com", Password = "CorrectPassword" }
            );

            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var response = okResult.Value as AuthResponse;
            Assert.NotNull(response);
            Assert.AreEqual("TOKEN123", response.Token);

            // Email service should have been called
            _emailMock.Verify(
                e =>
                    e.SendTemplatedAsync(
                        user.Email,
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        null,
                        null
                    ),
                Times.Once
            );
            _auditMock.Verify(
                a => a.LogAsync("User", user.Id, "Login", It.IsAny<string>(), user.Id, user.Email),
                Times.Once
            );
        }

        // ------------------ FORGOT PASSWORD TESTS ------------------ //
        [Test]
        public async Task ForgotPasswordRequest_EmptyEmail_ReturnsBadRequest()
        {
            var result = await _controller.ForgotPasswordRequest(
                new ResetPasswordRequest { Email = "" }
            );
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task ForgotPasswordRequest_NonExistingEmail_ReturnsOk_WithoutAuditOrEmail()
        {
            var result = await _controller.ForgotPasswordRequest(
                new ResetPasswordRequest { Email = "fake@test.com" }
            );
            Assert.IsInstanceOf<OkObjectResult>(result);

            _auditMock.Verify(
                a =>
                    a.LogAsync(
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<string>()
                    ),
                Times.Never
            );
            _emailMock.Verify(
                e =>
                    e.SendTemplatedAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        null,
                        null
                    ),
                Times.Never
            );
        }

        [Test]
        public async Task ForgotPasswordRequest_ValidEmail_CreatesAuditAndSendsEmails()
        {
            var user = new User
            {
                Id = 1,
                Email = "user@test.com",
                FullName = "End User",
                Role = UserRole.Employee,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
                IsActive = true,
            };
            var admin = new User
            {
                Id = 2,
                Email = "admin@test.com",
                FullName = "Admin",
                Role = UserRole.Admin,
                PasswordHash = "hash",
                IsActive = true,
            };

            _dbContext.Users.AddRange(user, admin);
            await _dbContext.SaveChangesAsync();

            var result = await _controller.ForgotPasswordRequest(
                new ResetPasswordRequest { Email = "user@test.com" }
            );
            Assert.IsInstanceOf<OkObjectResult>(result);

            _auditMock.Verify(
                a =>
                    a.LogAsync(
                        "User",
                        user.Id,
                        "PasswordResetRequest",
                        It.IsAny<string>(),
                        user.Id,
                        user.Email
                    ),
                Times.Once
            );
            _emailMock.Verify(
                e =>
                    e.SendTemplatedAsync(
                        admin.Email,
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        null,
                        null
                    ),
                Times.Once
            );
            _emailMock.Verify(
                e =>
                    e.SendTemplatedAsync(
                        user.Email,
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        null,
                        null
                    ),
                Times.Once
            );
        }

        // ------------------ CHANGE PASSWORD TESTS ------------------ //
        [Test]
        public async Task ChangePassword_UserNotFound_ReturnsNotFound()
        {
            // Simulate User Claims with non-existing userId
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "123") })
                ),
            };

            var result = await _controller.ChangePassword(
                new ChangePasswordDto { CurrentPassword = "oldpass", NewPassword = "newpass" }
            );
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
        {
            var user = new User
            {
                Id = 5,
                Email = "userX@test.com",
                FullName = "Wrong Pw User",
                Role = UserRole.Employee,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
                IsActive = true,
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "5") })
                ),
            };

            var result = await _controller.ChangePassword(
                new ChangePasswordDto
                {
                    CurrentPassword = "WrongPassword",
                    NewPassword = "NewPass123",
                }
            );
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task ChangePassword_CorrectPassword_ChangesHash_AndLogsAndEmails()
        {
            var user = new User
            {
                Id = 6,
                Email = "changer@test.com",
                FullName = "Change Pw User",
                Role = UserRole.Employee,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass"),
                IsActive = true,
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "6") })
                ),
            };

            var result = await _controller.ChangePassword(
                new ChangePasswordDto { CurrentPassword = "OldPass", NewPassword = "NewSecret123" }
            );
            Assert.IsInstanceOf<OkObjectResult>(result);

            var updatedUser = await _dbContext.Users.FindAsync(6);
            Assert.IsTrue(BCrypt.Net.BCrypt.Verify("NewSecret123", updatedUser.PasswordHash));
            _auditMock.Verify(
                a =>
                    a.LogAsync(
                        "UserPassword",
                        user.Id,
                        "Updated",
                        "Password changed",
                        user.Id,
                        user.Email
                    ),
                Times.Once
            );
            _emailMock.Verify(
                e =>
                    e.SendTemplatedAsync(
                        user.Email,
                        "Password Changed",
                        It.IsAny<string>(),
                        null,
                        null
                    ),
                Times.Once
            );
        }
    }
}
