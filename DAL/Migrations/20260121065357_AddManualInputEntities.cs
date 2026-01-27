using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddManualInputEntities : Migration
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
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_account_types", x => x.Id);
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
                name: "tbl_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionTypeId = table.Column<Guid>(type: "uuid", nullable: false),
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
                columns: new[] { "Id", "CreatedAt", "DisplayOrder", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111001"), new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(3803), 1, "Tiền mặt" },
                    { new Guid("11111111-1111-1111-1111-111111111002"), new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(4514), 2, "Tài khoản ngân hàng" },
                    { new Guid("11111111-1111-1111-1111-111111111003"), new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(4518), 3, "Thẻ tín dụng" },
                    { new Guid("11111111-1111-1111-1111-111111111004"), new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(4520), 4, "Tài khoản đầu tư" },
                    { new Guid("11111111-1111-1111-1111-111111111005"), new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(4522), 5, "Ví điện tử" },
                    { new Guid("11111111-1111-1111-1111-111111111006"), new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(4523), 6, "Khác" }
                });

            migrationBuilder.InsertData(
                table: "tbl_transaction_types",
                columns: new[] { "Id", "Color", "CreatedAt", "DisplayOrder", "IsIncome", "Name" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222001"), "#F87171", new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(8888), 1, false, "Chi tiêu" },
                    { new Guid("22222222-2222-2222-2222-222222222002"), "#34D399", new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(9670), 2, true, "Thu tiền" },
                    { new Guid("22222222-2222-2222-2222-222222222003"), "#FBBF24", new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(9674), 3, false, "Cho vay" },
                    { new Guid("22222222-2222-2222-2222-222222222004"), "#A78BFA", new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(9676), 4, true, "Đi vay" }
                });

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
                name: "IX_tbl_money_sources_AccountTypeId",
                table: "tbl_money_sources",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_money_sources_UserId",
                table: "tbl_money_sources",
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
        }
    }
}
