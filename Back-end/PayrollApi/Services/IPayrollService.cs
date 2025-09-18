using PayrollApi.Models;

namespace PayrollApi.Services
{
    public interface IPayrollService
    {
        Payslip ComputePayslipFromCTC(
            CTCStructure ctc,
            int year,
            int month,
            int lopDays,
            decimal? overrideAllowances,
            decimal? overrideDeductions
        );
    }
}
