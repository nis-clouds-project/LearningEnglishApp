using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    learned_words = table.Column<string>(type: "jsonb", nullable: false),
                    my_words = table.Column<string>(type: "jsonb", nullable: false),
                    user_ai_usage = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "words",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    translation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_custom = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_words", x => x.id);
                    table.ForeignKey(
                        name: "FK_words_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_words_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_learned_words",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    word_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_learned_words", x => new { x.user_id, x.word_id });
                    table.ForeignKey(
                        name: "FK_user_learned_words_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_learned_words_words_word_id",
                        column: x => x.word_id,
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_viewed_words",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    word_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_viewed_words", x => new { x.user_id, x.word_id });
                    table.ForeignKey(
                        name: "FK_user_viewed_words_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_viewed_words_words_word_id",
                        column: x => x.word_id,
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_name",
                table: "categories",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_user_learned_words_word_id",
                table: "user_learned_words",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_viewed_words_word_id",
                table: "user_viewed_words",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "ix_words_category_id",
                table: "words",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_words_text",
                table: "words",
                column: "text");

            migrationBuilder.CreateIndex(
                name: "ix_words_user_id",
                table: "words",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_learned_words");

            migrationBuilder.DropTable(
                name: "user_viewed_words");

            migrationBuilder.DropTable(
                name: "words");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
