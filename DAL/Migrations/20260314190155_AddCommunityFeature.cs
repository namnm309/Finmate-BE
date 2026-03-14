using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_community_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LikesCount = table.Column<int>(type: "integer", nullable: false),
                    CommentsCount = table.Column<int>(type: "integer", nullable: false),
                    SharesCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_community_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_community_posts_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_community_post_bookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_community_post_bookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_community_post_bookmarks_tbl_community_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "tbl_community_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_community_post_bookmarks_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_community_post_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_community_post_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_community_post_comments_tbl_community_post_comments_Par~",
                        column: x => x.ParentCommentId,
                        principalTable: "tbl_community_post_comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_community_post_comments_tbl_community_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "tbl_community_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_community_post_comments_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_community_post_likes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_community_post_likes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_community_post_likes_tbl_community_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "tbl_community_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_community_post_likes_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPostBookmarks_PostId_UserId",
                table: "tbl_community_post_bookmarks",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_community_post_bookmarks_UserId",
                table: "tbl_community_post_bookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPostComments_PostId",
                table: "tbl_community_post_comments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_community_post_comments_ParentCommentId",
                table: "tbl_community_post_comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_community_post_comments_UserId",
                table: "tbl_community_post_comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPostLikes_PostId_UserId",
                table: "tbl_community_post_likes",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_community_post_likes_UserId",
                table: "tbl_community_post_likes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_CreatedAt",
                table: "tbl_community_posts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_LikesCount",
                table: "tbl_community_posts",
                column: "LikesCount");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_UserId",
                table: "tbl_community_posts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_community_post_bookmarks");

            migrationBuilder.DropTable(
                name: "tbl_community_post_comments");

            migrationBuilder.DropTable(
                name: "tbl_community_post_likes");

            migrationBuilder.DropTable(
                name: "tbl_community_posts");
        }
    }
}
