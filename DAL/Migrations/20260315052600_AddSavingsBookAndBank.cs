using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsBookAndBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_banks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_savings_books",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DepositDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TermMonths = table.Column<int>(type: "integer", nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NonTermInterestRate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DaysInYearForInterest = table.Column<int>(type: "integer", nullable: false),
                    InterestPaymentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaturityOption = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceMoneySourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExcludeFromReports = table.Column<bool>(type: "boolean", nullable: false),
                    InitialBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaturityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_savings_books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_savings_books_tbl_banks_BankId",
                        column: x => x.BankId,
                        principalTable: "tbl_banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_savings_books_tbl_money_sources_SourceMoneySourceId",
                        column: x => x.SourceMoneySourceId,
                        principalTable: "tbl_money_sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tbl_savings_books_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tbl_banks",
                columns: new[] { "Id", "Code", "CreatedAt", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444001"), "VCB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)" },
                    { new Guid("44444444-4444-4444-4444-444444444002"), "BIDV", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)" },
                    { new Guid("44444444-4444-4444-4444-444444444003"), "CTG", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "Ngân hàng TMCP Công thương Việt Nam (VietinBank)" },
                    { new Guid("44444444-4444-4444-4444-444444444004"), "EIB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "Ngân hàng TMCP Xuất nhập khẩu Việt Nam (Eximbank)" },
                    { new Guid("44444444-4444-4444-4444-444444444005"), "AGRIBANK", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "Ngân hàng Nông nghiệp và Phát triển Nông thôn Việt Nam (Agribank)" },
                    { new Guid("44444444-4444-4444-4444-444444444006"), "TCB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, "Ngân hàng TMCP Kỹ thương Việt Nam (Techcombank)" },
                    { new Guid("44444444-4444-4444-4444-444444444007"), "MB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, "Ngân hàng TMCP Quân đội (MB Bank)" },
                    { new Guid("44444444-4444-4444-4444-444444444008"), "ACB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, true, "Ngân hàng TMCP Á Châu (ACB)" },
                    { new Guid("44444444-4444-4444-4444-444444444009"), "VPB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, true, "Ngân hàng TMCP Việt Nam Thịnh Vượng (VP Bank)" },
                    { new Guid("44444444-4444-4444-4444-444444444010"), "TPB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, true, "Ngân hàng TMCP Tiên Phong (TP Bank)" },
                    { new Guid("44444444-4444-4444-4444-444444444011"), "HDB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 11, true, "Ngân hàng TMCP Phát triển TP.HCM (HDBank)" },
                    { new Guid("44444444-4444-4444-4444-444444444012"), "STB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, true, "Ngân hàng TMCP Sài Gòn Thương Tín (Sacombank)" },
                    { new Guid("44444444-4444-4444-4444-444444444013"), "SHB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 13, true, "Ngân hàng TMCP Sài Gòn - Hà Nội (SHB)" },
                    { new Guid("44444444-4444-4444-4444-444444444014"), "LPB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 14, true, "Ngân hàng TMCP Bưu điện Liên Việt (LPBank)" },
                    { new Guid("44444444-4444-4444-4444-444444444015"), "OCB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 15, true, "Ngân hàng TMCP Phương Đông (OCB)" },
                    { new Guid("44444444-4444-4444-4444-444444444016"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 99, true, "Khác" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavingsBooks_UserId",
                table: "tbl_savings_books",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_savings_books_BankId",
                table: "tbl_savings_books",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_savings_books_SourceMoneySourceId",
                table: "tbl_savings_books",
                column: "SourceMoneySourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_savings_books");

            migrationBuilder.DropTable(
                name: "tbl_banks");
        }
    }
}
