using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixStaticSeedTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111001"),
                column: "CreatedAt",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111002"),
                column: "CreatedAt",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111003"),
                column: "CreatedAt",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111004"),
                column: "CreatedAt",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111005"),
                column: "CreatedAt",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111006"),
                column: "CreatedAt",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "tbl_transaction_types",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222001"),
                column: "CreatedAt",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "tbl_transaction_types",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222002"),
                column: "CreatedAt",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "tbl_transaction_types",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222003"),
                column: "CreatedAt",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "tbl_transaction_types",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222004"),
                column: "CreatedAt",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111001"),
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(3803));

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111002"),
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(4514));

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111003"),
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(4518));

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111004"),
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(4520));

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111005"),
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(4522));

            migrationBuilder.UpdateData(
                table: "tbl_account_types",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111006"),
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(4523));

            migrationBuilder.UpdateData(
                table: "tbl_transaction_types",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222001"),
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(8888));

            migrationBuilder.UpdateData(
                table: "tbl_transaction_types",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222002"),
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(9670));

            migrationBuilder.UpdateData(
                table: "tbl_transaction_types",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222003"),
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(9674));

            migrationBuilder.UpdateData(
                table: "tbl_transaction_types",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222004"),
                column: "CreatedAt",
                value: new DateTime(2026, 1, 21, 6, 53, 55, 101, DateTimeKind.Utc).AddTicks(9676));
        }
    }
}
