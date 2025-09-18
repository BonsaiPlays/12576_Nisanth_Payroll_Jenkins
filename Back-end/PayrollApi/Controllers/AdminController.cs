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
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;
        private readonly IAuditService _audit;

        public AdminController(AppDbContext db, IEmailService email, IAuditService auditService)
        {
            _db = db;
            _email = email;
            _audit = auditService;
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
        /// Retrieves a paginated list of users with optional search by email or full name.
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<PagedResult<UserListItem>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null
        )
        {
            var q = _db.Users.Include(u => u.Profile).ThenInclude(p => p.Department).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(u => u.Email.Contains(search) || u.FullName.Contains(search));

            var (total, items) = await q.OrderBy(u => u.Id)
                .Select(u => new UserListItem
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role.ToString(),
                    IsActive = u.IsActive,
                    Department =
                        u.Profile != null
                            ? u.Profile.Department != null
                                ? u.Profile.Department.Name
                                : null
                            : null,
                })
                .ToPagedAsync(page, pageSize);

            return Ok(
                new PagedResult<UserListItem>
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    Items = items,
                }
            );
        }

        /// <summary>
        /// Creates a skeletal user with a temporary password and sends welcome notification and email.
        /// </summary>
        [HttpPost("users")]
        public async Task<IActionResult> CreateSkeletalUser(CreateSkeletalUserRequest req)
        {
            if (req == null)
                return BadRequest("Request body is missing or invalid");

            if (!Enum.TryParse<UserRole>(req.Role, true, out var role))
                return BadRequest("Invalid role");
            if (await _db.Users.AnyAsync(u => u.Email == req.Email))
                return Conflict("Email already exists");

            var tempPassword = Guid.NewGuid().ToString("n")[..10] + "!a";
            var user = new User
            {
                Email = req.Email,
                FullName = req.FullName,
                Role = role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var profile = new EmployeeProfile { UserId = user.Id, DepartmentId = req.DepartmentId };
            _db.EmployeeProfiles.Add(profile);
            await _db.SaveChangesAsync();

            // Logging
            await _audit.LogAsync(
                "User",
                user.Id,
                "Created",
                $"Skeletal user {user.Email} created with role {role}",
                CurrentUserId,
                CurrentUserEmail
            );

            // Notification
            _db.Notifications.Add(
                new Notification
                {
                    UserId = user.Id,
                    Subject = "Welcome to Payroll",
                    Message =
                        $"Your password has been sent to the mail. Please change your password.",
                }
            );
            await _db.SaveChangesAsync();

            // Mail
            try
            {
                await _email.SendTemplatedAsync(
                    user.Email,
                    "Welcome to Payroll System",
                    $"Hi {user.FullName},<br/>Your account has been created.<br/><br/>"
                        + $"Your payroll account has been created. Temporary password: {tempPassword}<br/>"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
            }

            return Ok(
                new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    Role = user.Role.ToString(),
                    TempPassword = tempPassword,
                }
            );
        }

        /// <summary>
        /// Resets the password for the specified user and sends a temporary password via email and notification.
        /// </summary>
        [HttpPost("users/{userId}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            var tempPassword = Guid.NewGuid().ToString("n")[..10] + "!a";
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Logging
            await _audit.LogAsync(
                "User",
                user.Id,
                "PasswordReset",
                $"Password reset by admin, tempPassword issued",
                CurrentUserId,
                CurrentUserEmail
            );

            // Notification
            _db.Notifications.Add(
                new Notification
                {
                    UserId = user.Id,
                    Subject = "Password Reset",
                    Message = $"Your password has been reset. Temporary password: {tempPassword}",
                }
            );
            await _db.SaveChangesAsync();

            // Mail
            try
            {
                await _email.SendTemplatedAsync(
                    user.Email,
                    "Password Reset - Payroll System",
                    $"Hello {user.FullName},<br/>Your new temporary password is <b>{tempPassword}</b><br/>Please change it after logging in."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
            }

            return Ok(
                new
                {
                    Message = "Password reset successfully. New temporary password has been emailed.",
                }
            );
        }

        /// <summary>
        /// Updates the active status of a user and notifies them via email and in-app notification.
        /// </summary>
        [HttpPut("users/{userId}/status")]
        public async Task<IActionResult> SetActive(int userId, [FromQuery] bool active)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.IsActive = active;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Logging
            await _audit.LogAsync(
                "User",
                user.Id,
                "StatusChanged",
                $"User account {(active ? "activated" : "deactivated")}",
                CurrentUserId,
                CurrentUserEmail
            );

            // Notification
            _db.Notifications.Add(
                new Notification
                {
                    UserId = user.Id,
                    Subject = "Account Status Changed",
                    Message = $"Your account is now {(active ? "Active" : "Inactive")}",
                }
            );
            await _db.SaveChangesAsync();

            // Mail
            try
            {
                await _email.SendTemplatedAsync(
                    user.Email,
                    "Account Status Changed",
                    $"Hello {user.FullName},<br/>Your account has been set to <b>{(active ? "Active" : "Inactive")}</b>."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
            }

            return Ok(new { user.Id, user.IsActive });
        }

        /// <summary>
        /// Deletes a user and their associated profile, if any, with audit logging.
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _db
                .Users.Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound($"User with id {userId} not found");

            if (user.Profile != null)
                _db.EmployeeProfiles.Remove(user.Profile);

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            // Logging
            await _audit.LogAsync(
                "User",
                user.Id,
                "Deleted",
                $"User {user.Email} deleted by admin",
                CurrentUserId,
                CurrentUserEmail
            );

            return NoContent();
        }
    }
}
