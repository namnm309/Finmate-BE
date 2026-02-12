using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoryu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_categories_tbl_categories_ParentCategoryId",
                table: "tbl_categories");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_categories_tbl_categories_ParentCategoryId",
                table: "tbl_categories",
                column: "ParentCategoryId",
                principalTable: "tbl_categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_categories_tbl_categories_ParentCategoryId",
                table: "tbl_categories");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_categories_tbl_categories_ParentCategoryId",
                table: "tbl_categories",
                column: "ParentCategoryId",
                principalTable: "tbl_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
