using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TableNamesWillBePlural : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "image_of_the_day");

            migrationBuilder.DropPrimaryKey(
                name: "pk_request_log",
                table: "request_log");

            migrationBuilder.DropPrimaryKey(
                name: "pk_notification_to_admin",
                table: "notification_to_admin");

            migrationBuilder.RenameTable(
                name: "request_log",
                newName: "request_logs");

            migrationBuilder.RenameTable(
                name: "notification_to_admin",
                newName: "notifications_to_admin");

            migrationBuilder.AddPrimaryKey(
                name: "pk_request_logs",
                table: "request_logs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_notifications_to_admin",
                table: "notifications_to_admin",
                column: "id");

            migrationBuilder.CreateTable(
                name: "daily_images",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    alt_text = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    url_works = table.Column<bool>(type: "boolean", nullable: false),
                    do_i_prefer_to_display_this = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_images", x => x.id);
                },
                comment: "List of daily background images by Bing, NASA, etc. They are great");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_images");

            migrationBuilder.DropPrimaryKey(
                name: "pk_request_logs",
                table: "request_logs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_notifications_to_admin",
                table: "notifications_to_admin");

            migrationBuilder.RenameTable(
                name: "request_logs",
                newName: "request_log");

            migrationBuilder.RenameTable(
                name: "notifications_to_admin",
                newName: "notification_to_admin");

            migrationBuilder.AddPrimaryKey(
                name: "pk_request_log",
                table: "request_log",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_notification_to_admin",
                table: "notification_to_admin",
                column: "id");

            migrationBuilder.CreateTable(
                name: "image_of_the_day",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    alt_text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    do_i_prefer_to_display_this = table.Column<bool>(type: "boolean", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    url_works = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_image_of_the_day", x => x.id);
                },
                comment: "List of daily background images by Bing, NASA, etc. They are great");
        }
    }
}
