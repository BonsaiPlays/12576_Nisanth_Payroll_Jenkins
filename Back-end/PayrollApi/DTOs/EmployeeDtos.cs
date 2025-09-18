using PayrollApi.Models.Enums;

namespace PayrollApi.DTOs
{
    public class EmployeeProfileUpdate
    {
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class PayslipSummary
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal NetPay { get; set; }
        public ApprovalStatus Status { get; set; }
        public bool IsReleased { get; set; }
    }
}
