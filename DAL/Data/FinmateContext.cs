using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Data
{
    public class FinmateContext : DbContext
    {
        // Constructor cho DI (Dependency Injection)
        public FinmateContext(DbContextOptions<FinmateContext> options) : base(options)
        {
        }

        // Constructor mặc định cho testing/migration
        public FinmateContext()
        {
        }

        //========================================================================================================================
        //Khai báo entity 
        //Dbset biểu diễn 1 bảng của csdl 
        public DbSet<Users> Users { get; set; }
        public DbSet<AccountType> AccountTypes { get; set; }
        public DbSet<TransactionType> TransactionTypes { get; set; }
        public DbSet<MoneySource> MoneySources { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<PremiumSubscription> PremiumSubscriptions { get; set; }

        //Nếu muốn cấu hình chi tiết thêm thì overrive OnModelCreating
        //Nếu đã sử dụng [] trc các attribute thì có thể ko cần method này 
        //Nếu có 1 số cái phức tạp mà [] ko thể triển khai hết thì nên dùng method này 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users configuration
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasMany(u => u.MoneySources)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.Categories)
                    .WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.Contacts)
                    .WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.Transactions)
                    .WithOne(t => t.User)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.Goals)
                    .WithOne(g => g.User)
                    .HasForeignKey(g => g.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.PremiumSubscriptions)
                    .WithOne(p => p.User)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AccountType configuration
            modelBuilder.Entity<AccountType>(entity =>
            {
                entity.HasMany(a => a.MoneySources)
                    .WithOne(m => m.AccountType)
                    .HasForeignKey(m => m.AccountTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TransactionType configuration
            modelBuilder.Entity<TransactionType>(entity =>
            {
                entity.HasMany(t => t.Categories)
                    .WithOne(c => c.TransactionType)
                    .HasForeignKey(c => c.TransactionTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(t => t.Transactions)
                    .WithOne(tr => tr.TransactionType)
                    .HasForeignKey(tr => tr.TransactionTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // MoneySource configuration
            modelBuilder.Entity<MoneySource>(entity =>
            {
                entity.HasMany(m => m.Transactions)
                    .WithOne(t => t.MoneySource)
                    .HasForeignKey(t => t.MoneySourceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasMany(c => c.Transactions)
                    .WithOne(t => t.Category)
                    .HasForeignKey(t => t.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Contact configuration
            modelBuilder.Entity<Contact>(entity =>
            {
                entity.HasMany(c => c.Transactions)
                    .WithOne(t => t.Contact)
                    .HasForeignKey(t => t.ContactId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // PremiumSubscription configuration
            modelBuilder.Entity<PremiumSubscription>(entity =>
            {
                // Check constraint cho Plan
                entity.HasCheckConstraint("CK_PremiumSubscriptions_Plan", 
                    "Plan IN ('1-month', '6-month', '1-year')");

                // Indexes
                entity.HasIndex(p => p.UserId)
                    .HasDatabaseName("IX_PremiumSubscriptions_UserId");

                entity.HasIndex(p => p.IsActive)
                    .HasDatabaseName("IX_PremiumSubscriptions_IsActive");

                entity.HasIndex(p => p.ExpiresAt)
                    .HasDatabaseName("IX_PremiumSubscriptions_ExpiresAt");
            });

            // Transaction configuration
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(t => t.Amount)
                    .HasPrecision(18, 2);
            });

            var seedTimestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Seed data cho AccountType
            modelBuilder.Entity<AccountType>().HasData(
                new AccountType
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111001"),
                    Name = "Tiền mặt",
                    Icon = "account-balance-wallet",
                    Color = "#4CAF50",
                    DisplayOrder = 1,
                    CreatedAt = seedTimestamp
                },
                new AccountType
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111002"),
                    Name = "Tài khoản ngân hàng",
                    Icon = "account-balance",
                    Color = "#2196F3",
                    DisplayOrder = 2,
                    CreatedAt = seedTimestamp
                },
                new AccountType
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111003"),
                    Name = "Thẻ tín dụng",
                    Icon = "credit-card",
                    Color = "#FF9800",
                    DisplayOrder = 3,
                    CreatedAt = seedTimestamp
                },
                new AccountType
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111004"),
                    Name = "Tài khoản đầu tư",
                    Icon = "trending-up",
                    Color = "#9C27B0",
                    DisplayOrder = 4,
                    CreatedAt = seedTimestamp
                },
                new AccountType
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111005"),
                    Name = "Ví điện tử",
                    Icon = "wallet",
                    Color = "#E91E63",
                    DisplayOrder = 5,
                    CreatedAt = seedTimestamp
                },
                new AccountType
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111006"),
                    Name = "Khác",
                    Icon = "more-horiz",
                    Color = "#607D8B",
                    DisplayOrder = 6,
                    CreatedAt = seedTimestamp
                }
            );

            // Seed data cho TransactionType
            modelBuilder.Entity<TransactionType>().HasData(
                new TransactionType
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222001"),
                    Name = "Chi tiêu",
                    Color = "#F87171",
                    IsIncome = false,
                    DisplayOrder = 1,
                    CreatedAt = seedTimestamp
                },
                new TransactionType
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222002"),
                    Name = "Thu tiền",
                    Color = "#34D399",
                    IsIncome = true,
                    DisplayOrder = 2,
                    CreatedAt = seedTimestamp
                },
                new TransactionType
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222003"),
                    Name = "Cho vay",
                    Color = "#FBBF24",
                    IsIncome = false,
                    DisplayOrder = 3,
                    CreatedAt = seedTimestamp
                },
                new TransactionType
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222004"),
                    Name = "Đi vay",
                    Color = "#A78BFA",
                    IsIncome = true,
                    DisplayOrder = 4,
                    CreatedAt = seedTimestamp
                }
            );

            // Seed data cho Currency
            modelBuilder.Entity<Currency>().HasData(
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333001"),
                    Code = "VND",
                    Name = "Vietnamese Dong",
                    Symbol = "₫",
                    CountryCode = "VN",
                    DisplayOrder = 1,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333002"),
                    Code = "VGO",
                    Name = "Vietnamese Gold (SJC)",
                    Symbol = "chỉ",
                    CountryCode = "VN",
                    DisplayOrder = 2,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333003"),
                    Code = "USD",
                    Name = "United States Dollar",
                    Symbol = "$",
                    CountryCode = "US",
                    DisplayOrder = 3,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333004"),
                    Code = "CNY",
                    Name = "Chinese Yuan",
                    Symbol = "¥",
                    CountryCode = "CN",
                    DisplayOrder = 4,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333005"),
                    Code = "EUR",
                    Name = "Euro",
                    Symbol = "€",
                    CountryCode = "EU",
                    DisplayOrder = 5,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333006"),
                    Code = "GBP",
                    Name = "British Pound Sterling",
                    Symbol = "£",
                    CountryCode = "GB",
                    DisplayOrder = 6,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333007"),
                    Code = "JPY",
                    Name = "Japanese Yen",
                    Symbol = "¥",
                    CountryCode = "JP",
                    DisplayOrder = 7,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333008"),
                    Code = "CHF",
                    Name = "Swiss Franc",
                    Symbol = "Fr.",
                    CountryCode = "CH",
                    DisplayOrder = 8,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333009"),
                    Code = "AUD",
                    Name = "Australian Dollar",
                    Symbol = "$",
                    CountryCode = "AU",
                    DisplayOrder = 9,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333010"),
                    Code = "SGD",
                    Name = "Singapore Dollar",
                    Symbol = "$",
                    CountryCode = "SG",
                    DisplayOrder = 10,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333011"),
                    Code = "HKD",
                    Name = "Hong Kong Dollar",
                    Symbol = "$",
                    CountryCode = "HK",
                    DisplayOrder = 11,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333012"),
                    Code = "KRW",
                    Name = "South Korean Won",
                    Symbol = "₩",
                    CountryCode = "KR",
                    DisplayOrder = 12,
                    IsActive = true
                },
                new Currency
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333013"),
                    Code = "THB",
                    Name = "Thai Baht",
                    Symbol = "฿",
                    CountryCode = "TH",
                    DisplayOrder = 13,
                    IsActive = true
                }
            );
        }

    }
}
