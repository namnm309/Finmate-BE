using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddParentCategoryIdToCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentCategoryId",
                table: "tbl_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_categories_ParentCategoryId",
                table: "tbl_categories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_categories_tbl_categories_ParentCategoryId",
                table: "tbl_categories",
                column: "ParentCategoryId",
                principalTable: "tbl_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_categories_tbl_categories_ParentCategoryId",
                table: "tbl_categories");

            migrationBuilder.DropIndex(
                name: "IX_tbl_categories_ParentCategoryId",
                table: "tbl_categories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "tbl_categories");
        }
    }
}
