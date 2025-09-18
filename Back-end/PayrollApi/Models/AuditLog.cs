namespace PayrollApi.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string EntityType { get; set; } = default!;
        public int? EntityId { get; set; }
        public string Action { get; set; } = default!;
        public string? PerformedBy { get; set; }
        public int? PerformedById { get; set; }
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
        public string? Details { get; set; }
    }
}
