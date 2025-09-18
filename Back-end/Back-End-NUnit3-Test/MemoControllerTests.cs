/*
    NUnit Test Suite for MemoController

    Endpoints tested:

    GET /api/memos
    - Returns only memos belonging to the current user
    - Ignores memos from other users

    POST /api/memos
    - Successfully adds a new memo and returns it
    - Persists to database

    PUT /api/memos/{id}
    - Returns NotFound if memo with given ID does not exist for current user
    - Updates content and date of an existing memo

    DELETE /api/memos/{id}
    - Returns NotFound if memo with given ID does not exist for current user
    - Removes existing memo and returns Ok
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
using PayrollApi.Services;

namespace PayrollApi.Tests.Controllers
{
    [TestFixture]
    public class MemoControllerTests
    {
        private AppDbContext _db;
        private Mock<IEmailService> _email;
        private MemoController _controller;

        private User MakeTestUser(int id = 1)
        {
            return new User
            {
                Id = id,
                Email = $"user{id}@test.com",
                FullName = $"User{id}",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Secret123!"),
                Role = PayrollApi.Models.Enums.UserRole.Employee,
                IsActive = true,
            };
        }

        [SetUp]
        public void Setup()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(opts);
            _email = new Mock<IEmailService>();
            _controller = new MemoController(_db, _email.Object);

            // Fake claims with userId=1
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
        public async Task GetMemos_ReturnsOnlyCurrentUserMemos()
        {
            _db.Memos.Add(
                new Memo
                {
                    Id = 1,
                    UserId = 1,
                    Date = DateTime.UtcNow,
                    Content = "Mine",
                }
            );
            _db.Memos.Add(
                new Memo
                {
                    Id = 2,
                    UserId = 2,
                    Date = DateTime.UtcNow,
                    Content = "Other",
                }
            );
            await _db.SaveChangesAsync();

            var result = await _controller.GetMemos() as OkObjectResult;
            Assert.NotNull(result);
            var list = result.Value as List<Memo>;
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("Mine", list[0].Content);
        }

        [Test]
        public async Task AddMemo_AddsAndReturnsMemo()
        {
            var dto = new MemoDto { Date = DateTime.UtcNow, Content = "Hello" };
            var result = await _controller.AddMemo(dto) as OkObjectResult;
            Assert.NotNull(result);

            var memo = result.Value as Memo;
            Assert.AreEqual("Hello", memo.Content);

            Assert.AreEqual(1, _db.Memos.Count());
        }

        [Test]
        public async Task UpdateMemo_NotFound_Returns404()
        {
            var update = new Memo
            {
                Id = 99,
                Date = DateTime.UtcNow,
                Content = "Update",
            };
            var res = await _controller.UpdateMemo(99, update);
            Assert.IsInstanceOf<NotFoundResult>(res);
        }

        [Test]
        public async Task UpdateMemo_Existing_UpdatesContent()
        {
            var memo = new Memo
            {
                Id = 1,
                UserId = 1,
                Date = DateTime.UtcNow,
                Content = "Old",
            };
            _db.Memos.Add(memo);
            await _db.SaveChangesAsync();

            var update = new Memo
            {
                Id = 1,
                Date = DateTime.UtcNow.Date,
                Content = "NewContent",
            };

            var res = await _controller.UpdateMemo(1, update) as OkObjectResult;
            Assert.NotNull(res);
            var updated = res.Value as Memo;
            Assert.AreEqual("NewContent", updated.Content);
        }

        [Test]
        public async Task DeleteMemo_NotFound_Returns404()
        {
            var result = await _controller.DeleteMemo(123);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task DeleteMemo_RemovesMemo()
        {
            var memo = new Memo
            {
                Id = 1,
                UserId = 1,
                Date = DateTime.UtcNow,
                Content = "Delete",
            };
            _db.Memos.Add(memo);
            await _db.SaveChangesAsync();

            var result = await _controller.DeleteMemo(1);
            Assert.IsInstanceOf<OkResult>(result);
            Assert.AreEqual(0, _db.Memos.Count());
        }
    }
}
