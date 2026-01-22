using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddIconColorToAccountType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "tbl_account_types",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "tbl_account_types",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111001"),
                columns: new[] { "Color", "Icon" },
                values: new object[] { "#4CAF50", "account-balance-wallet" });

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111002"),
                columns: new[] { "Color", "Icon" },
                values: new object[] { "#2196F3", "account-balance" });

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111003"),
                columns: new[] { "Color", "Icon" },
                values: new object[] { "#FF9800", "credit-card" });

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111004"),
                columns: new[] { "Color", "Icon" },
                values: new object[] { "#9C27B0", "trending-up" });

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111005"),
                columns: new[] { "Color", "Icon" },
                values: new object[] { "#E91E63", "wallet" });

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111006"),
                columns: new[] { "Color", "Icon" },
                values: new object[] { "#607D8B", "more-horiz" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "tbl_account_types");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "tbl_account_types");
        }
    }
}
