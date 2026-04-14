using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumPlanConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_premium_plan_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PriceVnd = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    OriginalPriceVnd = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: true),
                    DiscountPercent = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_premium_plan_configs", x => x.Id);
                    table.CheckConstraint("CK_PremiumPlanConfigs_Plan", "\"Plan\" IN ('1-month', '6-month', '1-year')");
                });

            migrationBuilder.InsertData(
                table: "tbl_premium_plan_configs",
                columns: new[] { "Id", "CreatedAt", "DiscountPercent", "IsActive", "LastLoginAt", "OriginalPriceVnd", "Plan", "PriceVnd", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555001"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null, "1-month", 79000m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("55555555-5555-5555-5555-555555555002"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 18, true, null, 474000m, "6-month", 389000m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("55555555-5555-5555-5555-555555555003"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 25, true, null, 948000m, "1-year", 710000m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PremiumPlanConfigs_Plan",
                table: "tbl_premium_plan_configs",
                column: "Plan",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_premium_plan_configs");
        }
    }
}
