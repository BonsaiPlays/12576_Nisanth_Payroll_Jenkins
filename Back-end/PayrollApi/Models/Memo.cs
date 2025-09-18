using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayrollApi.Models
{
    public class Memo : AuditEntity
    {
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User? User { get; set; } = default!;

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(500)]
        public string? Content { get; set; }
    }
}
