using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsAdminWithRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm cột Role trước
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "tbl_users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Chuyển đổi dữ liệu: IsAdmin = true -> Role = 2 (Admin), IsAdmin = false -> Role = 0 (User)
            migrationBuilder.Sql(@"
                UPDATE tbl_users 
                SET ""Role"" = CASE 
                    WHEN ""IsAdmin"" = true THEN 2 
                    ELSE 0 
                END
            ");

            // Xóa cột IsAdmin sau khi đã chuyển đổi dữ liệu
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "tbl_users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "tbl_users");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "tbl_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
