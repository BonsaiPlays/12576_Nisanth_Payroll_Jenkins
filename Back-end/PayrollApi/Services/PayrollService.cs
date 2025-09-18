using PayrollApi.Models;

namespace PayrollApi.Services
{
    public class PayrollService : IPayrollService
    {
        public Payslip ComputePayslipFromCTC(
            CTCStructure ctc,
            int year,
            int month,
            int lopDays,
            decimal? overrideAllowances,
            decimal? overrideDeductions
        )
        {
            // Monthly basics
            var monthlyBasic = ctc.Basic / 12m;
            var monthlyHRA = ctc.HRA / 12m;

            // Sum of collections
            var totalAllowances = ctc.Allowances?.Sum(a => a.Amount) ?? 0m;
            var totalDeductions = ctc.Deductions?.Sum(d => d.Amount) ?? 0m;

            var monthlyAllowances = (overrideAllowances ?? totalAllowances) / 12m;
            var monthlyDeductions = (overrideDeductions ?? totalDeductions) / 12m;

            // LOP handling
            var totalPayable = monthlyBasic + monthlyHRA + monthlyAllowances;
            var dailyRate = totalPayable / 30m;
            var lopAmount = dailyRate * lopDays;

            var preTax = totalPayable - lopAmount - monthlyDeductions;
            if (preTax < 0)
                preTax = 0;

            var tax = preTax * (ctc.TaxPercent / 100m);
            var net = preTax - tax;

            // Populate Payslip with line items
            var slip = new Payslip
            {
                Year = year,
                Month = month,
                Basic = Math.Round(monthlyBasic, 2),
                HRA = Math.Round(monthlyHRA, 2),
                TaxDeducted = Math.Round(tax, 2),
                LOPDays = lopDays,
                NetPay = Math.Round(net, 2),
                Status = 0,
                IsReleased = false,
            };

            // Fill Allowance snapshot
            foreach (var a in ctc.Allowances ?? new List<CTCAllowance>())
            {
                slip.AllowanceItems.Add(
                    new PayslipAllowance
                    {
                        Label = a.Label,
                        Amount = Math.Round(a.Amount / 12m, 2), // monthly portion
                    }
                );
            }

            // Fill Deduction snapshot
            foreach (var d in ctc.Deductions ?? new List<CTCDeduction>())
            {
                slip.DeductionItems.Add(
                    new PayslipDeduction { Label = d.Label, Amount = Math.Round(d.Amount / 12m, 2) }
                );
            }

            // Add LOP deduction as its own line item
            if (lopAmount > 0)
            {
                slip.DeductionItems.Add(
                    new PayslipDeduction { Label = "LOP", Amount = Math.Round(lopAmount, 2) }
                );
            }

            return slip;
        }
    }
}
