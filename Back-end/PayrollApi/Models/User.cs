using System.ComponentModel.DataAnnotations;
using PayrollApi.Models.Enums;

namespace PayrollApi.Models
{
    public class User : AuditEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; } = default!;

        [Required]
        public string PasswordHash { get; set; } = default!;

        [Required, MaxLength(120)]
        public string FullName { get; set; } = default!;

        [Required]
        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public EmployeeProfile? Profile { get; set; }
    }
}
