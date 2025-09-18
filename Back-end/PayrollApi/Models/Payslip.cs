using System.ComponentModel.DataAnnotations.Schema;
using PayrollApi.Models.Enums;

namespace PayrollApi.Models
{
    public class Payslip : AuditEntity
    {
        public int Id { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        [ForeignKey(nameof(EmployeeProfile))]
        public int EmployeeProfileId { get; set; }
        public EmployeeProfile EmployeeProfile { get; set; } = default!;

        public int Year { get; set; }
        public int Month { get; set; } // 1-12

        public decimal Basic { get; set; }
        public decimal HRA { get; set; }
        public ICollection<PayslipAllowance> AllowanceItems { get; set; } =
            new List<PayslipAllowance>();
        public ICollection<PayslipDeduction> DeductionItems { get; set; } =
            new List<PayslipDeduction>();
        public decimal TaxDeducted { get; set; }
        public int LOPDays { get; set; }
        public decimal NetPay { get; set; }
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; }
        public string? FilePath { get; set; }

        // public bool IsApproved { get; set; } = false;
        public bool IsReleased { get; set; } = false;
    }

    public class PayslipAllowance
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Payslip))]
        public int PayslipId { get; set; }
        public Payslip Payslip { get; set; } = default!;

        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class PayslipDeduction
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Payslip))]
        public int PayslipId { get; set; }
        public Payslip Payslip { get; set; } = default!;

        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
