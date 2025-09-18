using PayrollApi.Data;
using PayrollApi.Models;

namespace PayrollApi.Services
{
    public interface IAuditService
    {
        Task LogAsync(
            string entityType,
            int? entityId,
            string action,
            string? details,
            int? performedById,
            string? performedByEmail
        );
    }

    public class AuditService : IAuditService
    {
        private readonly AppDbContext _db;

        public AuditService(AppDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(
            string entityType,
            int? entityId,
            string action,
            string? details,
            int? performedById,
            string? performedByEmail
        )
        {
            var log = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                Details = details,
                PerformedById = performedById,
                PerformedBy = performedByEmail,
                PerformedAt = DateTime.UtcNow,
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
    }
}
