using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_account_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_account_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_transaction_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsIncome = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_transaction_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClerkUserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Occupation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrencyPreference = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LanguagePreference = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPremium = table.Column<bool>(type: "boolean", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_categories_tbl_categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "tbl_categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_categories_tbl_transaction_types_TransactionTypeId",
                        column: x => x.TransactionTypeId,
                        principalTable: "tbl_transaction_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_categories_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_contacts_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TargetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_goals_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_money_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_money_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_money_sources_tbl_account_types_AccountTypeId",
                        column: x => x.AccountTypeId,
                        principalTable: "tbl_account_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_money_sources_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_premium_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_premium_subscriptions", x => x.Id);
                    table.CheckConstraint("CK_PremiumSubscriptions_Plan", "\"Plan\" IN ('1-month', '6-month', '1-year')");
                    table.ForeignKey(
                        name: "FK_tbl_premium_subscriptions_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MoneySourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsBorrowingForThis = table.Column<bool>(type: "boolean", nullable: false),
                    IsFee = table.Column<bool>(type: "boolean", nullable: false),
                    ExcludeFromReport = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_transactions_tbl_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "tbl_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_transactions_tbl_contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "tbl_contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tbl_transactions_tbl_money_sources_MoneySourceId",
                        column: x => x.MoneySourceId,
                        principalTable: "tbl_money_sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_transactions_tbl_transaction_types_TransactionTypeId",
                        column: x => x.TransactionTypeId,
                        principalTable: "tbl_transaction_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_transactions_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tbl_account_types",
                columns: new[] { "Id", "Color", "CreatedAt", "DisplayOrder", "Icon", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111001"), "#4CAF50", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "account-balance-wallet", "Tiền mặt" },
                    { new Guid("11111111-1111-1111-1111-111111111002"), "#2196F3", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "account-balance", "Tài khoản ngân hàng" },
                    { new Guid("11111111-1111-1111-1111-111111111003"), "#FF9800", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "credit-card", "Thẻ tín dụng" },
                    { new Guid("11111111-1111-1111-1111-111111111004"), "#9C27B0", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, "trending-up", "Tài khoản đầu tư" },
                    { new Guid("11111111-1111-1111-1111-111111111005"), "#E91E63", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, "wallet", "Ví điện tử" },
                    { new Guid("11111111-1111-1111-1111-111111111006"), "#607D8B", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, "more-horiz", "Khác" }
                });

            migrationBuilder.InsertData(
                table: "tbl_currencies",
                columns: new[] { "Id", "Code", "CountryCode", "DisplayOrder", "IsActive", "Name", "Symbol" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333001"), "VND", "VN", 1, true, "Vietnamese Dong", "₫" },
                    { new Guid("33333333-3333-3333-3333-333333333002"), "VGO", "VN", 2, true, "Vietnamese Gold (SJC)", "chỉ" },
                    { new Guid("33333333-3333-3333-3333-333333333003"), "USD", "US", 3, true, "United States Dollar", "$" },
                    { new Guid("33333333-3333-3333-3333-333333333004"), "CNY", "CN", 4, true, "Chinese Yuan", "¥" },
                    { new Guid("33333333-3333-3333-3333-333333333005"), "EUR", "EU", 5, true, "Euro", "€" },
                    { new Guid("33333333-3333-3333-3333-333333333006"), "GBP", "GB", 6, true, "British Pound Sterling", "£" },
                    { new Guid("33333333-3333-3333-3333-333333333007"), "JPY", "JP", 7, true, "Japanese Yen", "¥" },
                    { new Guid("33333333-3333-3333-3333-333333333008"), "CHF", "CH", 8, true, "Swiss Franc", "Fr." },
                    { new Guid("33333333-3333-3333-3333-333333333009"), "AUD", "AU", 9, true, "Australian Dollar", "$" },
                    { new Guid("33333333-3333-3333-3333-333333333010"), "SGD", "SG", 10, true, "Singapore Dollar", "$" },
                    { new Guid("33333333-3333-3333-3333-333333333011"), "HKD", "HK", 11, true, "Hong Kong Dollar", "$" },
                    { new Guid("33333333-3333-3333-3333-333333333012"), "KRW", "KR", 12, true, "South Korean Won", "₩" },
                    { new Guid("33333333-3333-3333-3333-333333333013"), "THB", "TH", 13, true, "Thai Baht", "฿" }
                });

            migrationBuilder.InsertData(
                table: "tbl_transaction_types",
                columns: new[] { "Id", "Color", "CreatedAt", "DisplayOrder", "IsIncome", "Name" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222001"), "#F87171", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, "Chi tiêu" },
                    { new Guid("22222222-2222-2222-2222-222222222002"), "#34D399", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "Thu tiền" },
                    { new Guid("22222222-2222-2222-2222-222222222003"), "#FBBF24", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, false, "Cho vay" },
                    { new Guid("22222222-2222-2222-2222-222222222004"), "#A78BFA", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "Đi vay" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_categories_ParentCategoryId",
                table: "tbl_categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_categories_TransactionTypeId",
                table: "tbl_categories",
                column: "TransactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_categories_UserId",
                table: "tbl_categories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_contacts_UserId",
                table: "tbl_contacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_goals_UserId",
                table: "tbl_goals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_money_sources_AccountTypeId",
                table: "tbl_money_sources",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_money_sources_UserId",
                table: "tbl_money_sources",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PremiumSubscriptions_ExpiresAt",
                table: "tbl_premium_subscriptions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PremiumSubscriptions_IsActive",
                table: "tbl_premium_subscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PremiumSubscriptions_UserId",
                table: "tbl_premium_subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_transactions_CategoryId",
                table: "tbl_transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_transactions_ContactId",
                table: "tbl_transactions",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_transactions_MoneySourceId",
                table: "tbl_transactions",
                column: "MoneySourceId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_transactions_TransactionTypeId",
                table: "tbl_transactions",
                column: "TransactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_transactions_UserId",
                table: "tbl_transactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_currencies");

            migrationBuilder.DropTable(
                name: "tbl_goals");

            migrationBuilder.DropTable(
                name: "tbl_premium_subscriptions");

            migrationBuilder.DropTable(
                name: "tbl_transactions");

            migrationBuilder.DropTable(
                name: "tbl_categories");

            migrationBuilder.DropTable(
                name: "tbl_contacts");

            migrationBuilder.DropTable(
                name: "tbl_money_sources");

            migrationBuilder.DropTable(
                name: "tbl_transaction_types");

            migrationBuilder.DropTable(
                name: "tbl_account_types");

            migrationBuilder.DropTable(
                name: "tbl_users");
        }
    }
}
