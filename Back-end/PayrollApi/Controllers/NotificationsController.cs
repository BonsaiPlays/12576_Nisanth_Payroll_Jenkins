using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollApi.Data;

namespace PayrollApi.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Route("api/admin/notifications")]
    [Route("api/hr/notifications")]
    [Route("api/manager/notifications")]
    [Route("api/employee/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db) => _db = db;

        private int? CurrentUserId
        {
            get
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(idClaim))
                    return null;
                return int.Parse(idClaim);
            }
        }

        /// <summary>
        /// Retrieves paginated notifications for the current user, or for a specified user if admin.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            [FromQuery] bool unreadOnly = false
        )
        {
            try
            {
                if (!CurrentUserId.HasValue)
                    return Unauthorized("No user identity");

                var q = _db.Notifications.AsQueryable();

                q = q.Where(n => n.UserId == CurrentUserId.Value);

                if (unreadOnly)
                    q = q.Where(n => !n.IsRead);

                var total = await q.CountAsync();

                var items = await q.OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        n.Id,
                        n.Subject,
                        n.Message,
                        n.IsRead,
                        n.CreatedAt,
                    })
                    .ToListAsync();

                return Ok(
                    new
                    {
                        Page = page,
                        PageSize = pageSize,
                        Total = total,
                        Items = items,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { Error = "Internal Server Error", Detail = ex.Message }
                );
            }
        }

        /// <summary>
        /// Marks a specific notification as read for the current user.
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!CurrentUserId.HasValue)
                return Unauthorized();

            var n = await _db.Notifications.FirstOrDefaultAsync(x =>
                x.Id == id && x.UserId == CurrentUserId.Value
            );
            if (n == null)
                return NotFound("Notification not found");

            n.IsRead = true;
            n.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Notification marked as read" });
        }

        /// <summary>
        /// Marks all unread notifications as read for the current user.
        /// </summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (!CurrentUserId.HasValue)
                return Unauthorized();

            var items = await _db
                .Notifications.Where(n => n.UserId == CurrentUserId.Value && !n.IsRead)
                .ToListAsync();

            foreach (var n in items)
            {
                n.IsRead = true;
                n.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok(new { Message = "All notifications marked as read" });
        }

        /// <summary>
        /// Returns the count of unread notifications for the current user.
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!CurrentUserId.HasValue)
                return Unauthorized();

            var count = await _db.Notifications.CountAsync(n =>
                n.UserId == CurrentUserId.Value && !n.IsRead
            );

            return Ok(new { Count = count });
        }
    }
}
