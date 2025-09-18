using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollApi.Data;
using PayrollApi.DTOs;
using PayrollApi.Models;
using PayrollApi.Models.Enums;
using PayrollApi.Services;

namespace PayrollApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IJwtService _jwt;
        private readonly IEmailService _email;
        private readonly IAuditService _audit;

        public AuthController(
            AppDbContext db,
            IJwtService jwt,
            IEmailService email,
            IAuditService audit
        )
        {
            _db = db;
            _jwt = jwt;
            _email = email;
            _audit = audit;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token if credentials are valid.
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
        {
            var user = await _db
                .Users.Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (
                user == null
                || !user.IsActive
                || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)
            )
                return Unauthorized("Invalid credentials");

            var (token, exp) = _jwt.Generate(user);
            var profileCompleted =
                user.Profile != null
                && (user.Profile.Address != null || user.Profile.DepartmentId != null);

            // Logging
            await _audit.LogAsync(
                "User",
                user.Id,
                "Login",
                $"Successful login.",
                user.Id,
                user.Email
            );
            await _db.SaveChangesAsync();

            // Mail
            var loginTime = DateTime.UtcNow;
            await _email.SendTemplatedAsync(
                user.Email,
                "New Login Detected",
                $"Hello {user.FullName},<br/>"
                    + $"A new login was detected on your account.<br/>"
                    + $"<b>Time (UTC):</b> {loginTime:yyyy-MM-dd HH:mm:ss}<br/>"
                    + $"If this wasn’t you, please change your password immediately."
            );

            return Ok(
                new AuthResponse
                {
                    Token = token,
                    Role = user.Role.ToString(),
                    UserId = user.Id,
                    FullName = user.FullName,
                    ProfileCompleted = profileCompleted,
                    ExpiresAtUtc = exp,
                }
            );
        }

        /// <summary>
        /// Creates a password reset request for the specified email address.
        /// Logs the request in the audit logs, notifies administrators,
        /// and emails the user a confirmation.
        /// </summary>
        [HttpPost("forgot-password/request")]
        public async Task<IActionResult> ForgotPasswordRequest([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("Email is required.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            if (user == null)
                return Ok(new { Message = "If the email exists, a reset request will be sent." });

            //Audit log
            await _audit.LogAsync(
                "User",
                user.Id,
                "PasswordResetRequest",
                $"User {user.Email} requested a password reset.",
                user.Id,
                user.Email
            );
            await _db.SaveChangesAsync();

            //Notify admin(s)
            var admins = await _db.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();
            foreach (var admin in admins)
            {
                _db.Notifications.Add(
                    new Notification
                    {
                        UserId = admin.Id,
                        Subject = "Password Reset Request Raised",
                        Message =
                            $"User {user.FullName} ({user.Email}) has requested a password reset.",
                    }
                );

                await _email.SendTemplatedAsync(
                    admin.Email,
                    "Password Reset Request Raised",
                    $"Admin,<br/>User {user.FullName} ({user.Email}) has raised a password reset request at {DateTime.UtcNow:dd-MMM-yyyy HH:mm} UTC."
                );
            }
            await _db.SaveChangesAsync();

            //Notify user themselves
            await _email.SendTemplatedAsync(
                user.Email,
                "Password Reset Request Submitted",
                $"Hello {user.FullName},<br/>Your request for password reset has been received successfully.<br/>Please wait until an administrator processes your request."
            );

            return Ok(new { Message = "Reset request processed." });
        }

        /// <summary>
        /// Change Employee Password
        /// </summary>
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto req)
        {
            // Get current user id from claims
            var userId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            var u = await _db.Users.FindAsync(userId);
            if (u == null)
                return NotFound("User not found");

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, u.PasswordHash))
                return BadRequest("Incorrect current password");

            // Hash new password with BCrypt
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _db.SaveChangesAsync();

            // Audit log
            await _audit.LogAsync(
                "UserPassword",
                u.Id,
                "Updated",
                "Password changed",
                u.Id,
                u.Email
            );

            // Notification email
            await _email.SendTemplatedAsync(
                u.Email,
                "Password Changed",
                $"Hi {u.FullName},<br/>Your account password has been updated successfully."
            );

            return Ok(new { Message = "Password changed successfully" });
        }
    }
}
