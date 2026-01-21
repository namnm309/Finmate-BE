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

            // Transaction configuration
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(t => t.Amount)
                    .HasPrecision(18, 2);
            });

            // Seed data cho AccountType
            modelBuilder.Entity<AccountType>().HasData(
                new AccountType { Id = Guid.Parse("11111111-1111-1111-1111-111111111001"), Name = "Tiền mặt", DisplayOrder = 1 },
                new AccountType { Id = Guid.Parse("11111111-1111-1111-1111-111111111002"), Name = "Tài khoản ngân hàng", DisplayOrder = 2 },
                new AccountType { Id = Guid.Parse("11111111-1111-1111-1111-111111111003"), Name = "Thẻ tín dụng", DisplayOrder = 3 },
                new AccountType { Id = Guid.Parse("11111111-1111-1111-1111-111111111004"), Name = "Tài khoản đầu tư", DisplayOrder = 4 },
                new AccountType { Id = Guid.Parse("11111111-1111-1111-1111-111111111005"), Name = "Ví điện tử", DisplayOrder = 5 },
                new AccountType { Id = Guid.Parse("11111111-1111-1111-1111-111111111006"), Name = "Khác", DisplayOrder = 6 }
            );

            // Seed data cho TransactionType
            modelBuilder.Entity<TransactionType>().HasData(
                new TransactionType { Id = Guid.Parse("22222222-2222-2222-2222-222222222001"), Name = "Chi tiêu", Color = "#F87171", IsIncome = false, DisplayOrder = 1 },
                new TransactionType { Id = Guid.Parse("22222222-2222-2222-2222-222222222002"), Name = "Thu tiền", Color = "#34D399", IsIncome = true, DisplayOrder = 2 },
                new TransactionType { Id = Guid.Parse("22222222-2222-2222-2222-222222222003"), Name = "Cho vay", Color = "#FBBF24", IsIncome = false, DisplayOrder = 3 },
                new TransactionType { Id = Guid.Parse("22222222-2222-2222-2222-222222222004"), Name = "Đi vay", Color = "#A78BFA", IsIncome = true, DisplayOrder = 4 }
            );
        }

    }
}
