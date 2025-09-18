using PayrollApi.Models.Enums;

namespace PayrollApi.DTOs
{
    public class CTCLineItem
    {
        public string Label { get; set; } = string.Empty; // e.g. "Travel Allowance"
        public decimal Amount { get; set; }
    }

    public class CTCRequest
    {
        public decimal Basic { get; set; }
        public decimal HRA { get; set; }
        public List<CTCLineItem> AllowanceItems { get; set; } = new();
        public List<CTCLineItem> DeductionItems { get; set; } = new();
        public decimal TaxPercent { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public List<int> EmployeeUserIds { get; set; } = new();
    }

    public class CTCBatchRequest
    {
        public List<int> EmployeeUserIds { get; set; } = new();
        public decimal Basic { get; set; }
        public decimal HRA { get; set; }
        public List<CTCLineItem> AllowanceItems { get; set; } = new();
        public List<CTCLineItem> DeductionItems { get; set; } = new();
        public decimal TaxPercent { get; set; }
        public DateTime EffectiveFrom { get; set; }
    }

    public class PayslipCreateRequest
    {
        public int EmployeeUserId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int LOPDays { get; set; }
        public decimal? OverridesAllowances { get; set; }
        public decimal? OverridesDeductions { get; set; }
    }

    public class PayslipFilter
    {
        public int? EmployeeUserId { get; set; } 
        public int? DepartmentId { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CTCResponse
    {
        public int Id { get; set; }
        public int EmployeeProfileId { get; set; }
        public decimal Basic { get; set; }
        public decimal HRA { get; set; }
        public List<CTCComponentDto> AllowanceItems { get; set; } = new();
        public List<CTCComponentDto> DeductionItems { get; set; } = new();
        public decimal Allowances => AllowanceItems.Sum(a => a.Amount);
        public decimal Deductions => DeductionItems.Sum(d => d.Amount);
        public decimal TaxPercent { get; set; }
        public decimal GrossCTC { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public bool IsApproved { get; set; }
    }

    public class CTCBatchResult
    {
        public int EmployeeId { get; set; }
        public string Employee { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Created / Conflict / Error
        public string? Message { get; set; }
    }

    public class CTCComponentDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class PayslipResponse
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Basic { get; set; }
        public decimal HRA { get; set; }
        public List<PayslipItemDto> AllowanceItems { get; set; } = new();
        public List<PayslipItemDto> DeductionItems { get; set; } = new();
        public decimal Allowances => AllowanceItems.Sum(a => a.Amount);
        public decimal Deductions => DeductionItems.Sum(d => d.Amount);
        public decimal TaxDeducted { get; set; }
        public int LOPDays { get; set; }
        public decimal NetPay { get; set; }
        public ApprovalStatus Status { get; set; }
        public bool IsReleased { get; set; }
    }

    public class PayslipItemDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class CompareMonthsResponse
    {
        public PeriodDto PeriodA { get; set; }
        public PeriodDto PeriodB { get; set; }
        public decimal Difference { get; set; }
        public decimal PercentChange { get; set; }
    }

    public class PeriodDto
    {
        public int year1 { get; set; }
        public int month1 { get; set; }
        public decimal TotalNet { get; set; }
    }
}
