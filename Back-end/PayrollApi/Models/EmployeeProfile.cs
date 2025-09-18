using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayrollApi.Models
{
    public class EmployeeProfile : AuditEntity
    {
        public int Id { get; set; }

        [Required, ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [MaxLength(50)]
        public string? EmployeeCode { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public ICollection<CTCStructure> CTCStructures { get; set; } = new List<CTCStructure>();
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = default!;
        public string NewPassword { get; set; } = default!;
    }
}
