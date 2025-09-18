using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net;
using PayrollApi.Data;
using PayrollApi.Models;
using PayrollApi.Models.Enums;

namespace PayrollApi.SeedData
{
    public static class SeedTestData
    {
        private static readonly string[] FirstNames = new[]
        {
            "Aarav",
            "Vivaan",
            "Aditya",
            "Arjun",
            "Krishna",
            "Saanvi",
            "Ananya",
            "Diya",
            "Ishita",
            "Priya",
        };

        private static readonly string[] LastNames = new[]
        {
            "Sharma",
            "Verma",
            "Reddy",
            "Patel",
            "Nair",
            "Menon",
            "Singh",
            "Yadav",
            "Rao",
            "Chowdhury",
        };

        private static readonly Random Rng = new Random();

        /// <summary>
        /// Run at app startup after migrations. Seeds 50 users + CTCs + payslips.
        /// </summary>
        public static void Seed(AppDbContext db)
        {
            // Skip if already seeded
            if (db.Users.Any(u => u.Role != UserRole.Admin))
                return;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password@123");
            var users = new List<User>();
            var profiles = new List<EmployeeProfile>();
            var ctcs = new List<CTCStructure>();
            var allowances = new List<CTCAllowance>();
            var deductions = new List<CTCDeduction>();
            var payslips = new List<Payslip>();
            var payslipAllowances = new List<PayslipAllowance>();
            var payslipDeductions = new List<PayslipDeduction>();

            // 1 HR Manager, 3 HR, 46 Employees
            var roles = new List<UserRole> { UserRole.HRManager };
            roles.AddRange(Enumerable.Repeat(UserRole.HR, 3));
            roles.AddRange(Enumerable.Repeat(UserRole.Employee, 46));

            for (int i = 0; i < 50; i++)
            {
                string first = FirstNames[Rng.Next(FirstNames.Length)];
                string last = LastNames[Rng.Next(LastNames.Length)];
                string fullName = $"{first} {last}";
                string email = $"{first.ToLower()}.{last.ToLower()}{i}@example.in";
                string phone = $"{Rng.Next(7, 9)}{Rng.Next(100000000, 999999999)}";

                var user = new User
                {
                    Id = i + 100,
                    Email = email,
                    FullName = fullName,
                    Role = roles[i],
                    PasswordHash = passwordHash,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };
                users.Add(user);

                var profile = new EmployeeProfile
                {
                    Id = i + 200,
                    UserId = user.Id,
                    DepartmentId = (i % 9) + 1,
                    Address = $"House {Rng.Next(1, 300)}, Random Street, City",
                    Phone = phone,
                    EmployeeCode = $"EMP{i + 1:D4}",
                };
                profiles.Add(profile);

                // Historical CTCs for 2020–2024
                for (int yr = 2020; yr <= 2024; yr++)
                {
                    var ctcId = (i * 1000) + yr; // unique enough
                    var basic = 300000 + (Rng.Next(20, 50) * 1000);
                    var hra = (int)(basic * 0.4m);
                    var gross = basic + hra + 50000;

                    var ctc = new CTCStructure
                    {
                        Id = ctcId,
                        EmployeeProfileId = profile.Id,
                        Basic = basic,
                        HRA = hra,
                        GrossCTC = gross,
                        TaxPercent = 10,
                        Status = ApprovalStatus.Approved,
                        IsApproved = true,
                        EffectiveFrom = new DateTime(yr, 1, 1),
                        EffectiveTo = new DateTime(yr, 12, 31),
                        CreatedByUserId = user.Id,
                        CreatedAt = new DateTime(yr, 1, 1),
                    };
                    ctcs.Add(ctc);

                    allowances.Add(
                        new CTCAllowance
                        {
                            Id = ctcId * 10 + 1,
                            CTCStructureId = ctc.Id,
                            Label = "Transport Allowance",
                            Amount = 30000,
                        }
                    );
                    deductions.Add(
                        new CTCDeduction
                        {
                            Id = ctcId * 10 + 2,
                            CTCStructureId = ctc.Id,
                            Label = "Provident Fund",
                            Amount = 20000,
                        }
                    );
                }

                // Active 2025 CTC
                var activeCtcId = (i * 1000) + 2025;
                var basic25 = 400000 + (Rng.Next(20, 50) * 1000);
                var hra25 = (int)(basic25 * 0.4m);
                var gross25 = basic25 + hra25 + 60000;

                var ctc2025 = new CTCStructure
                {
                    Id = activeCtcId,
                    EmployeeProfileId = profile.Id,
                    Basic = basic25,
                    HRA = hra25,
                    GrossCTC = gross25,
                    TaxPercent = 12,
                    Status = ApprovalStatus.Approved,
                    IsApproved = true,
                    EffectiveFrom = new DateTime(2025, 1, 1),
                    EffectiveTo = new DateTime(2025, 12, 31),
                    CreatedByUserId = user.Id,
                    CreatedAt = new DateTime(2025, 1, 1),
                };
                ctcs.Add(ctc2025);

                allowances.Add(
                    new CTCAllowance
                    {
                        Id = activeCtcId * 10 + 1,
                        CTCStructureId = ctc2025.Id,
                        Label = "Special Allowance",
                        Amount = 60000,
                    }
                );
                deductions.Add(
                    new CTCDeduction
                    {
                        Id = activeCtcId * 10 + 2,
                        CTCStructureId = ctc2025.Id,
                        Label = "PF",
                        Amount = 40000,
                    }
                );

                // Payslips 2020–2024 + Jan–Aug 2025 → Approved & Released
                int psId = i * 10000; // large unique id base
                foreach (int yr in Enumerable.Range(2020, 6)) // 2020–2025
                {
                    int maxMonth = yr == 2025 ? 8 : 12;
                    for (int m = 1; m <= maxMonth; m++)
                    {
                        psId++;
                        var net = 50000 + Rng.Next(5000, 20000);

                        var ps = new Payslip
                        {
                            Id = psId,
                            EmployeeProfileId = profile.Id,
                            Year = yr,
                            Month = m,
                            Basic = basic25 / 12,
                            HRA = hra25 / 12,
                            TaxDeducted = 2000,
                            LOPDays = 0,
                            NetPay = net,
                            Status = ApprovalStatus.Approved,
                            IsReleased = true,
                            CreatedByUserId = user.Id,
                            CreatedAt = new DateTime(yr, m, 1),
                        };
                        payslips.Add(ps);

                        payslipAllowances.Add(
                            new PayslipAllowance
                            {
                                Id = psId * 10 + 1,
                                PayslipId = ps.Id,
                                Label = "Allowance",
                                Amount = 4000,
                            }
                        );
                        payslipDeductions.Add(
                            new PayslipDeduction
                            {
                                Id = psId * 10 + 2,
                                PayslipId = ps.Id,
                                Label = "PF",
                                Amount = 2000,
                            }
                        );
                    }
                }

                // September 2025 → Pending
                psId++;
                payslips.Add(
                    new Payslip
                    {
                        Id = psId,
                        EmployeeProfileId = profile.Id,
                        Year = 2025,
                        Month = 9,
                        Basic = basic25 / 12,
                        HRA = hra25 / 12,
                        TaxDeducted = 2000,
                        LOPDays = 0,
                        NetPay = 60000,
                        Status = ApprovalStatus.Pending,
                        IsReleased = false,
                        CreatedByUserId = user.Id,
                        CreatedAt = new DateTime(2025, 9, 1),
                    }
                );
            }

            db.Users.AddRange(users);
            db.EmployeeProfiles.AddRange(profiles);
            db.CTCStructures.AddRange(ctcs);
            db.CTCAllowances.AddRange(allowances);
            db.CTCDeductions.AddRange(deductions);
            db.Payslips.AddRange(payslips);
            db.PayslipAllowances.AddRange(payslipAllowances);
            db.PayslipDeductions.AddRange(payslipDeductions);

            db.SaveChanges();
        }
    }
}
