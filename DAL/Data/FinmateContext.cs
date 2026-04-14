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
        public DbSet<CommunityPost> CommunityPosts { get; set; }
        public DbSet<CommunityPostLike> CommunityPostLikes { get; set; }
        public DbSet<CommunityPostBookmark> CommunityPostBookmarks { get; set; }
        public DbSet<CommunityPostComment> CommunityPostComments { get; set; }
        public DbSet<UserFollow> UserFollows { get; set; }
        public DbSet<Bank> Banks { get; set; }
        public DbSet<SavingsBook> SavingsBooks { get; set; }
        public DbSet<PremiumPlanConfig> PremiumPlanConfigs { get; set; }
        public DbSet<PremiumOrder> PremiumOrders { get; set; }
        public DbSet<SepayWebhookEvent> SepayWebhookEvents { get; set; }
        public DbSet<AppDownloadConfig> AppDownloadConfigs { get; set; }

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

                entity.HasMany(u => u.PremiumOrders)
                    .WithOne(o => o.User)
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.CommunityPosts)
                    .WithOne(p => p.User)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.CommunityPostLikes)
                    .WithOne(l => l.User)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.CommunityPostBookmarks)
                    .WithOne(b => b.User)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.CommunityPostComments)
                    .WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserFollow configuration
            modelBuilder.Entity<UserFollow>(entity =>
            {
                entity.HasIndex(f => new { f.FollowerId, f.FollowingId })
                    .IsUnique()
                    .HasDatabaseName("IX_UserFollows_FollowerId_FollowingId");

                entity.HasOne(f => f.Follower)
                    .WithMany()
                    .HasForeignKey(f => f.FollowerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Following)
                    .WithMany()
                    .HasForeignKey(f => f.FollowingId)
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
                // Quan hệ ParentCategory - Children sẽ được cấu hình theo convention
                // dựa trên ParentCategoryId, ParentCategory và Children trong Category model.
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
                    "\"Plan\" IN ('1-month', '6-month', '1-year')");

                // Indexes
                entity.HasIndex(p => p.UserId)
                    .HasDatabaseName("IX_PremiumSubscriptions_UserId");

                entity.HasIndex(p => p.IsActive)
                    .HasDatabaseName("IX_PremiumSubscriptions_IsActive");

                entity.HasIndex(p => p.ExpiresAt)
                    .HasDatabaseName("IX_PremiumSubscriptions_ExpiresAt");
            });

            // PremiumPlanConfig configuration
            modelBuilder.Entity<PremiumPlanConfig>(entity =>
            {
                entity.HasCheckConstraint("CK_PremiumPlanConfigs_Plan", "\"Plan\" IN ('1-month', '6-month', '1-year')");
                entity.HasIndex(p => p.Plan)
                    .IsUnique()
                    .HasDatabaseName("IX_PremiumPlanConfigs_Plan");

                entity.Property(p => p.PriceVnd).HasPrecision(18, 0);
                entity.Property(p => p.OriginalPriceVnd).HasPrecision(18, 0);
            });

            // PremiumOrder configuration
            modelBuilder.Entity<PremiumOrder>(entity =>
            {
                entity.HasCheckConstraint("CK_PremiumOrders_Plan",
                    "\"Plan\" IN ('1-month', '6-month', '1-year')");
                entity.HasCheckConstraint("CK_PremiumOrders_Status",
                    "\"Status\" IN ('Pending', 'Paid', 'Expired', 'Cancelled')");

                entity.Property(p => p.AmountVnd).HasPrecision(18, 0);

                entity.HasIndex(p => p.UserId)
                    .HasDatabaseName("IX_PremiumOrders_UserId");

                entity.HasIndex(p => p.Status)
                    .HasDatabaseName("IX_PremiumOrders_Status");

                entity.HasIndex(p => p.PaymentCode)
                    .IsUnique()
                    .HasDatabaseName("IX_PremiumOrders_PaymentCode");
            });

            modelBuilder.Entity<SepayWebhookEvent>(entity =>
            {
                entity.Property(p => p.TransferAmount).HasPrecision(18, 0);

                entity.HasIndex(p => p.SepayId)
                    .IsUnique()
                    .HasDatabaseName("IX_SepayWebhookEvents_SepayId");

                entity.HasIndex(p => p.ReferenceCode)
                    .HasDatabaseName("IX_SepayWebhookEvents_ReferenceCode");
            });

            modelBuilder.Entity<AppDownloadConfig>(entity =>
            {
                entity.Property(p => p.IosUrl).HasMaxLength(2048);
                entity.Property(p => p.AndroidUrl).HasMaxLength(2048);
            });

            // CommunityPost configuration
            modelBuilder.Entity<CommunityPost>(entity =>
            {
                entity.Property(p => p.Category)
                    .HasMaxLength(50);

                entity.Property(p => p.Content)
                    .HasMaxLength(2000);

                entity.HasIndex(p => p.CreatedAt)
                    .HasDatabaseName("IX_CommunityPosts_CreatedAt");

                entity.HasIndex(p => p.LikesCount)
                    .HasDatabaseName("IX_CommunityPosts_LikesCount");

                entity.HasIndex(p => p.UserId)
                    .HasDatabaseName("IX_CommunityPosts_UserId");
            });

            // CommunityPostLike configuration
            modelBuilder.Entity<CommunityPostLike>(entity =>
            {
                entity.HasIndex(l => new { l.PostId, l.UserId })
                    .IsUnique()
                    .HasDatabaseName("IX_CommunityPostLikes_PostId_UserId");

                entity.HasOne(l => l.Post)
                    .WithMany(p => p.Likes)
                    .HasForeignKey(l => l.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // CommunityPostBookmark configuration
            modelBuilder.Entity<CommunityPostBookmark>(entity =>
            {
                entity.HasIndex(b => new { b.PostId, b.UserId })
                    .IsUnique()
                    .HasDatabaseName("IX_CommunityPostBookmarks_PostId_UserId");

                entity.HasOne(b => b.Post)
                    .WithMany(p => p.Bookmarks)
                    .HasForeignKey(b => b.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // CommunityPostComment configuration
            modelBuilder.Entity<CommunityPostComment>(entity =>
            {
                entity.Property(c => c.Content)
                    .HasMaxLength(1000);

                entity.HasIndex(c => c.PostId)
                    .HasDatabaseName("IX_CommunityPostComments_PostId");

                entity.HasOne(c => c.Post)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.ParentComment)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(c => c.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Transaction configuration
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(t => t.Amount)
                    .HasPrecision(18, 2);
            });

            // SavingsBook configuration
            modelBuilder.Entity<SavingsBook>(entity =>
            {
                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Bank)
                    .WithMany()
                    .HasForeignKey(s => s.BankId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.SourceMoneySource)
                    .WithMany()
                    .HasForeignKey(s => s.SourceMoneySourceId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(s => s.UserId)
                    .HasDatabaseName("IX_SavingsBooks_UserId");
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

            // Seed data cho Bank (ngân hàng VN)
            var bankSeedTimestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            modelBuilder.Entity<Bank>().HasData(
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444001"), Name = "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)", Code = "VCB", DisplayOrder = 1, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444002"), Name = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)", Code = "BIDV", DisplayOrder = 2, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444003"), Name = "Ngân hàng TMCP Công thương Việt Nam (VietinBank)", Code = "CTG", DisplayOrder = 3, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444004"), Name = "Ngân hàng TMCP Xuất nhập khẩu Việt Nam (Eximbank)", Code = "EIB", DisplayOrder = 4, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444005"), Name = "Ngân hàng Nông nghiệp và Phát triển Nông thôn Việt Nam (Agribank)", Code = "AGRIBANK", DisplayOrder = 5, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444006"), Name = "Ngân hàng TMCP Kỹ thương Việt Nam (Techcombank)", Code = "TCB", DisplayOrder = 6, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444007"), Name = "Ngân hàng TMCP Quân đội (MB Bank)", Code = "MB", DisplayOrder = 7, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444008"), Name = "Ngân hàng TMCP Á Châu (ACB)", Code = "ACB", DisplayOrder = 8, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444009"), Name = "Ngân hàng TMCP Việt Nam Thịnh Vượng (VP Bank)", Code = "VPB", DisplayOrder = 9, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444010"), Name = "Ngân hàng TMCP Tiên Phong (TP Bank)", Code = "TPB", DisplayOrder = 10, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444011"), Name = "Ngân hàng TMCP Phát triển TP.HCM (HDBank)", Code = "HDB", DisplayOrder = 11, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444012"), Name = "Ngân hàng TMCP Sài Gòn Thương Tín (Sacombank)", Code = "STB", DisplayOrder = 12, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444013"), Name = "Ngân hàng TMCP Sài Gòn - Hà Nội (SHB)", Code = "SHB", DisplayOrder = 13, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444014"), Name = "Ngân hàng TMCP Bưu điện Liên Việt (LPBank)", Code = "LPB", DisplayOrder = 14, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444015"), Name = "Ngân hàng TMCP Phương Đông (OCB)", Code = "OCB", DisplayOrder = 15, IsActive = true, CreatedAt = bankSeedTimestamp },
                new Bank { Id = Guid.Parse("44444444-4444-4444-4444-444444444016"), Name = "Khác", Code = null, DisplayOrder = 99, IsActive = true, CreatedAt = bankSeedTimestamp }
            );

            // Seed data cho PremiumPlanConfig (giống migration AddPremiumPlanConfigs)
            modelBuilder.Entity<PremiumPlanConfig>().HasData(
                new PremiumPlanConfig
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555001"),
                    Plan = "1-month",
                    PriceVnd = 79000m,
                    OriginalPriceVnd = null,
                    DiscountPercent = null,
                    IsActive = true,
                    CreatedAt = seedTimestamp,
                    UpdatedAt = seedTimestamp,
                    LastLoginAt = null
                },
                new PremiumPlanConfig
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555002"),
                    Plan = "6-month",
                    PriceVnd = 389000m,
                    OriginalPriceVnd = 474000m,
                    DiscountPercent = 18,
                    IsActive = true,
                    CreatedAt = seedTimestamp,
                    UpdatedAt = seedTimestamp,
                    LastLoginAt = null
                },
                new PremiumPlanConfig
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555003"),
                    Plan = "1-year",
                    PriceVnd = 710000m,
                    OriginalPriceVnd = 948000m,
                    DiscountPercent = 25,
                    IsActive = true,
                    CreatedAt = seedTimestamp,
                    UpdatedAt = seedTimestamp,
                    LastLoginAt = null
                }
            );
        }

    }
}
