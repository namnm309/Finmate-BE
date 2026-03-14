using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm unique index cho ClerkUserId (non-null, trimmed)
            migrationBuilder.CreateIndex(
                name: "IX_tbl_users_ClerkUserId_Unique",
                table: "tbl_users",
                column: "ClerkUserId",
                unique: true,
                filter: "\"ClerkUserId\" IS NOT NULL AND TRIM(\"ClerkUserId\") <> ''");

            // Thêm unique index cho Email (normalized: trimmed, lowercase)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ""IX_tbl_users_Email_Normalized_Unique""
                ON tbl_users (LOWER(TRIM(""Email"")))
                WHERE ""Email"" IS NOT NULL 
                  AND TRIM(""Email"") <> '' 
                  AND LOWER(TRIM(""Email"")) NOT LIKE '%@placeholder.local';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_users_ClerkUserId_Unique",
                table: "tbl_users");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_tbl_users_Email_Normalized_Unique"";");
        }
    }
}
