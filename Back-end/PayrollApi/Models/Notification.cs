namespace PayrollApi.Models
{
    public class Notification : AuditEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Subject { get; set; } = default!;
        public string Message { get; set; } = default!;
        public bool IsRead { get; set; } = false;
    }
}
