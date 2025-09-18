using System.ComponentModel.DataAnnotations.Schema;
using PayrollApi.Models.Enums;

namespace PayrollApi.Models
{
    public class CTCAllowance
    {
        public int Id { get; set; }

        [ForeignKey(nameof(CTCStructure))]
        public int CTCStructureId { get; set; }
        public CTCStructure CTCStructure { get; set; } = default!;
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class CTCDeduction
    {
        public int Id { get; set; }

        [ForeignKey(nameof(CTCStructure))]
        public int CTCStructureId { get; set; }
        public CTCStructure CTCStructure { get; set; } = default!;
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class CTCStructure : AuditEntity
    {
        public int Id { get; set; }
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        [ForeignKey(nameof(EmployeeProfile))]
        public int EmployeeProfileId { get; set; }
        public EmployeeProfile EmployeeProfile { get; set; } = default!;
        public decimal Basic { get; set; }
        public decimal HRA { get; set; }
        public ICollection<CTCAllowance> Allowances { get; set; } = new List<CTCAllowance>();
        public ICollection<CTCDeduction> Deductions { get; set; } = new List<CTCDeduction>();
        public decimal TaxPercent { get; set; }
        public decimal GrossCTC { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public bool IsApproved { get; set; } = false;
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; }
    }
}
