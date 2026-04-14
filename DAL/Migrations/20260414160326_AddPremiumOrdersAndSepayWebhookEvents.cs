using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumOrdersAndSepayWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_premium_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AmountVnd = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    PaymentCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SepayTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    BankGateway = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AccountNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastWebhookContent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_premium_orders", x => x.Id);
                    table.CheckConstraint("CK_PremiumOrders_Plan", "\"Plan\" IN ('1-month', '6-month', '1-year')");
                    table.CheckConstraint("CK_PremiumOrders_Status", "\"Status\" IN ('Pending', 'Paid', 'Expired', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_tbl_premium_orders_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_sepay_webhook_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SepayId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    TransferType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TransferAmount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    ReferenceCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RawPayload = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_sepay_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PremiumOrders_PaymentCode",
                table: "tbl_premium_orders",
                column: "PaymentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PremiumOrders_Status",
                table: "tbl_premium_orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PremiumOrders_UserId",
                table: "tbl_premium_orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SepayWebhookEvents_ReferenceCode",
                table: "tbl_sepay_webhook_events",
                column: "ReferenceCode");

            migrationBuilder.CreateIndex(
                name: "IX_SepayWebhookEvents_SepayId",
                table: "tbl_sepay_webhook_events",
                column: "SepayId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_premium_orders");

            migrationBuilder.DropTable(
                name: "tbl_sepay_webhook_events");
        }
    }
}
