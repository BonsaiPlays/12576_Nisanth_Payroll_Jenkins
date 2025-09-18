/*
    NUnit Test Suite for AdminController:

    GET /users
    - Empty DB returns empty paged result
    - Returns paged users properly
    - Search by Email or FullName returns filtered results

    POST /users
    - Null request returns BadRequest
    - Invalid role string returns BadRequest
    - Duplicate email returns Conflict
    - Valid user creation succeeds, logs, notifies, and emails

    POST /users/{id}/reset-password
    - Invalid userId returns NotFound
    - Valid user resets password, creates log, notification, and email

    PUT /users/{id}/status
    - Invalid userId returns NotFound
    - Valid user deactivation works, logs, notifies, and emails
    - Valid user activation works

    DELETE /users/{id}
    - Invalid userId returns NotFound
    - Valid user without profile deletes user
    - Valid user with profile deletes user + profile, logs deletion
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

namespace PayrollApi.Tests.Controllers
{
    [TestFixture]
    public class AdminControllerTests
    {
        private AppDbContext _dbContext;
        private Mock<IEmailService> _emailMock;
        private Mock<IAuditService> _auditMock;
        private AdminController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            _emailMock = new Mock<IEmailService>();
            _auditMock = new Mock<IAuditService>();

            _controller = new AdminController(_dbContext, _emailMock.Object, _auditMock.Object);

            // Simulate authenticated admin user (CurrentUserId = 1)
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "1"),
                            new Claim(ClaimTypes.Email, "admin@test.com"),
                            new Claim(ClaimTypes.Role, "Admin"),
                        }
                    )
                ),
            };
        }

        [TearDown]
        public void TearDown() => _dbContext.Dispose();

        // --- Example Tests ---

        [Test]
        public async Task GetUsers_EmptyDb_ReturnsEmptyResult()
        {
            var result = await _controller.GetUsers();

            var ok = result.Result as OkObjectResult;
            Assert.NotNull(ok);

            var paged = ok.Value as PagedResult<UserListItem>;
            Assert.NotNull(paged);
            Assert.AreEqual(0, paged.Total);
            Assert.IsEmpty(paged.Items);
        }

        [Test]
        public async Task CreateSkeletalUser_InvalidRole_ReturnsBadRequest()
        {
            var request = new CreateSkeletalUserRequest
            {
                Email = "test@test.com",
                FullName = "Test User",
                Role = "InvalidRole",
            };

            var result = await _controller.CreateSkeletalUser(request);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task ResetUserPassword_NonExistentUser_ReturnsNotFound()
        {
            var result = await _controller.ResetUserPassword(999);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task SetActive_InvalidUser_ReturnsNotFound()
        {
            var result = await _controller.SetActive(123, true);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task DeleteUser_NonExistentUser_ReturnsNotFound()
        {
            var result = await _controller.DeleteUser(555);
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }
    }
}
