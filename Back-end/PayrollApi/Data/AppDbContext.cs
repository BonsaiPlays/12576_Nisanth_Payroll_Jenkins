using Microsoft.EntityFrameworkCore;
using PayrollApi.Models;
using PayrollApi.Models.Enums;

namespace PayrollApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options)
            : base(options) { }

        // DbSets
        public DbSet<User> Users => Set<User>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
        public DbSet<CTCStructure> CTCStructures => Set<CTCStructure>();
        public DbSet<CTCAllowance> CTCAllowances => Set<CTCAllowance>();
        public DbSet<CTCDeduction> CTCDeductions => Set<CTCDeduction>();
        public DbSet<Payslip> Payslips => Set<Payslip>();
        public DbSet<PayslipAllowance> PayslipAllowances => Set<PayslipAllowance>();
        public DbSet<PayslipDeduction> PayslipDeductions => Set<PayslipDeduction>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Memo> Memos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // Ignore string props so EF does not create extra columns
            modelBuilder.Entity<CTCStructure>().Ignore(c => c.CreatedBy);
            modelBuilder.Entity<CTCStructure>().Ignore(c => c.UpdatedBy);
            modelBuilder.Entity<Payslip>().Ignore(p => p.CreatedBy);
            modelBuilder.Entity<Payslip>().Ignore(p => p.UpdatedBy);

            // User → Profile (1‑1)
            modelBuilder
                .Entity<User>()
                .HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<EmployeeProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // EmployeeProfile → CTCStructures (1‑many, to allow history)
            modelBuilder
                .Entity<EmployeeProfile>()
                .HasMany(p => p.CTCStructures)
                .WithOne(c => c.EmployeeProfile)
                .HasForeignKey(c => c.EmployeeProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // CTCStructure → Allowances
            modelBuilder
                .Entity<CTCStructure>()
                .HasMany(c => c.Allowances)
                .WithOne(a => a.CTCStructure)
                .HasForeignKey(a => a.CTCStructureId)
                .OnDelete(DeleteBehavior.Cascade);

            // CTCStructure → Deductions
            modelBuilder
                .Entity<CTCStructure>()
                .HasMany(c => c.Deductions)
                .WithOne(d => d.CTCStructure)
                .HasForeignKey(d => d.CTCStructureId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payslip → AllowanceItems
            modelBuilder
                .Entity<Payslip>()
                .HasMany(p => p.AllowanceItems)
                .WithOne(a => a.Payslip)
                .HasForeignKey(a => a.PayslipId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payslip → DeductionItems
            modelBuilder
                .Entity<Payslip>()
                .HasMany(p => p.DeductionItems)
                .WithOne(d => d.Payslip)
                .HasForeignKey(d => d.PayslipId)
                .OnDelete(DeleteBehavior.Cascade);

            // CTCStructure → CreatedByUser (many-to-1, optional)
            modelBuilder
                .Entity<CTCStructure>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payslip → CreatedByUser (many-to-1, optional)
            modelBuilder
                .Entity<Payslip>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Departments
            modelBuilder
                .Entity<Department>()
                .HasData(
                    new Department { Id = 1, Name = "Engineering" },
                    new Department { Id = 2, Name = "Finance" },
                    new Department { Id = 3, Name = "Marketing" },
                    new Department { Id = 4, Name = "Sales" },
                    new Department { Id = 5, Name = "IT Support" },
                    new Department { Id = 6, Name = "Operations" },
                    new Department { Id = 7, Name = "R&D" },
                    new Department { Id = 8, Name = "Customer Service" },
                    new Department { Id = 9, Name = "HR" }
                );

            // Seed Admin user (password: Admin@123)
            var admin = new User
            {
                Id = 1,
                Email = "nisanthsaru.oto@gmail.com",
                FullName = "System Admin",
                Role = UserRole.Admin,
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                CreatedAt = DateTime.UtcNow,
            };
            modelBuilder.Entity<User>().HasData(admin);

            base.OnModelCreating(modelBuilder);
        }
    }
}
