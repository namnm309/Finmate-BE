using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_currencies");
        }
    }
}
