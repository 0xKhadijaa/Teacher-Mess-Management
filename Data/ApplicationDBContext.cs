using MessManagementSystem.Models.Shared;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MessManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Shared entities
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        // Billing entities
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillIssue> BillIssues { get; set; }

        // App settings
        public DbSet<AppSetting> AppSettings { get; set; }

        // Notifications
        public DbSet<Notification> Notifications { get; set; }

        // Auth
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure MenuItem.Date is unique (one menu per day)
            modelBuilder.Entity<MenuItem>()
                .HasIndex(m => m.Date)
                .IsUnique();

            // Configure BillIssue relationships
            modelBuilder.Entity<BillIssue>(entity =>
            {
                entity.HasOne(bi => bi.Teacher)
                      .WithMany()
                      .HasForeignKey(bi => bi.TeacherId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(bi => bi.Bill)
                      .WithMany()
                      .HasForeignKey(bi => bi.BillId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(bi => bi.IssueDescription)
                      .HasMaxLength(500)
                      .IsRequired();
            });

            // Configure Bill decimal precision
            modelBuilder.Entity<Bill>(entity =>
            {
                entity.Property(b => b.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(b => b.AmountPaid).HasColumnType("decimal(18,2)");
                entity.Property(b => b.PreviousDues).HasColumnType("decimal(18,2)");
            });

            // ✅ CRITICAL: Configure Notification → ApplicationUser relationship
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => n.CreatedAt);
                entity.Property(n => n.Title).HasMaxLength(200).IsRequired();
                entity.Property(n => n.Message).IsRequired();
            });

            // Seed default app settings
            modelBuilder.Entity<AppSetting>().HasData(
                new AppSetting { Id = 1, Key = "MealRate", Value = "200.00" },
                new AppSetting { Id = 2, Key = "UtilityCharge", Value = "150.00" }
            );
        }
    }
}