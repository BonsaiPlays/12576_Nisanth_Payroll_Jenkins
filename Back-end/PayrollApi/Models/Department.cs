using System.ComponentModel.DataAnnotations;

namespace PayrollApi.Models
{
    public class Department : AuditEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = default!;
    }
}
