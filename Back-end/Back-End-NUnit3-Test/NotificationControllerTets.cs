/*
    NUnit Test Suite for NotificationsController

    Endpoints:

    GET /notifications
    - Returns unauthorized if user identity missing
    - Returns only current user's notifications, paginated

    PUT /notifications/{id}/read
    - Unauthorized if no user identity
    - NotFound if notification not belonging to user
    - Marks a specific notification as read and returns Ok

    PUT /notifications/read-all
    - Unauthorized if no user identity
    - Marks all current user's notifications as read and returns Ok

    GET /notifications/unread-count
    - Unauthorized if no user identity
    - Returns count of unread notifications for current user
*/

using System;
using System.Collections.Generic;
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
    public class NotificationsControllerTests
    {
        private AppDbContext _db;
        private NotificationsController _controller;

        [SetUp]
        public void Setup()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(opts);
            _controller = new NotificationsController(_db);

            // Fake identity for userId = 1
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "1"),
                            new Claim(ClaimTypes.Email, "user1@test.com"),
                            new Claim(ClaimTypes.Role, "Employee"),
                        }
                    )
                ),
            };
        }

        [TearDown]
        public void Cleanup() => _db.Dispose();

        [Test]
        public async Task GetNotifications_WithoutIdentity_ReturnsUnauthorized()
        {
            _controller.ControllerContext.HttpContext = new DefaultHttpContext();
            var result = await _controller.GetNotifications();
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task GetNotifications_ReturnsCurrentUserOnly()
        {
            _db.Notifications.Add(
                new Notification
                {
                    Id = 1,
                    UserId = 1,
                    Subject = "Hello",
                    Message = "Mine",
                }
            );
            _db.Notifications.Add(
                new Notification
                {
                    Id = 2,
                    UserId = 2,
                    Subject = "Skip",
                    Message = "Other",
                }
            );
            await _db.SaveChangesAsync();

            var result = await _controller.GetNotifications() as OkObjectResult;
            Assert.NotNull(result);

            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            Assert.That(json, Does.Contain("Hello"));
            Assert.That(json, Does.Not.Contain("Skip"));
        }

        [Test]
        public async Task MarkAsRead_NotificationNotFound_Returns404()
        {
            var result = await _controller.MarkAsRead(999);
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task MarkAsRead_MarksNotification()
        {
            _db.Notifications.Add(
                new Notification
                {
                    Id = 10,
                    UserId = 1,
                    IsRead = false,
                    Subject = "A",
                    Message = "X",
                }
            );
            await _db.SaveChangesAsync();

            var result = await _controller.MarkAsRead(10) as OkObjectResult;
            Assert.NotNull(result);

            var n = await _db.Notifications.FindAsync(10);
            Assert.IsTrue(n.IsRead);
        }

        [Test]
        public async Task MarkAllAsRead_MarksAll()
        {
            _db.Notifications.Add(
                new Notification
                {
                    Id = 21,
                    UserId = 1,
                    IsRead = false,
                    Subject = "A",
                    Message = "X",
                }
            );
            _db.Notifications.Add(
                new Notification
                {
                    Id = 22,
                    UserId = 1,
                    IsRead = false,
                    Subject = "B",
                    Message = "Y",
                }
            );
            _db.Notifications.Add(
                new Notification
                {
                    Id = 23,
                    UserId = 2,
                    IsRead = false,
                    Subject = "C",
                    Message = "Z",
                }
            );
            await _db.SaveChangesAsync();

            var result = await _controller.MarkAllAsRead() as OkObjectResult;
            Assert.NotNull(result);

            Assert.IsTrue(_db.Notifications.Where(n => n.UserId == 1).All(n => n.IsRead));
            Assert.IsFalse(_db.Notifications.First(n => n.UserId == 2).IsRead);
        }

        [Test]
        public async Task GetUnreadCount_ReturnsCount()
        {
            _db.Notifications.Add(
                new Notification
                {
                    Id = 31,
                    UserId = 1,
                    IsRead = false,
                    Subject = "U1",
                    Message = "M1",
                }
            );
            _db.Notifications.Add(
                new Notification
                {
                    Id = 32,
                    UserId = 1,
                    IsRead = true,
                    Subject = "U2",
                    Message = "M2",
                }
            );
            _db.Notifications.Add(
                new Notification
                {
                    Id = 33,
                    UserId = 2,
                    IsRead = false,
                    Subject = "XX",
                    Message = "YY",
                }
            );
            await _db.SaveChangesAsync();

            var result = await _controller.GetUnreadCount() as OkObjectResult;
            Assert.NotNull(result);

            var dict = System.Text.Json.JsonSerializer.Serialize(result.Value);
            Assert.That(dict, Does.Contain("\"Count\":1"));
        }

        [Test]
        public async Task GetUnreadCount_NoIdentity_ReturnsUnauthorized()
        {
            _controller.ControllerContext.HttpContext = new DefaultHttpContext();
            var result = await _controller.GetUnreadCount();
            Assert.IsInstanceOf<UnauthorizedResult>(result);
        }
    }
}
