using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FirstMigrationForAnImprovedDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emails_to_admin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    email_address = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_emails_to_admin", x => x.id);
                },
                comment: "Emails that have been sent to the admin via the website ui");

            migrationBuilder.CreateTable(
                name: "image_of_the_day",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    alt_text = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    url_works = table.Column<bool>(type: "boolean", nullable: false),
                    do_i_prefer_to_display_this = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_image_of_the_day", x => x.id);
                },
                comment: "List of daily background images by Bing, NASA, etc. They are great");

            migrationBuilder.CreateTable(
                name: "request_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_agent = table.Column<string>(type: "text", nullable: false),
                    accept_language = table.Column<string>(type: "text", nullable: false),
                    platform = table.Column<string>(type: "text", nullable: false),
                    webdriver = table.Column<bool>(type: "boolean", nullable: false),
                    device_memory = table.Column<string>(type: "text", nullable: false),
                    hardware_concurrency = table.Column<string>(type: "text", nullable: false),
                    max_touch_points = table.Column<int>(type: "integer", nullable: false),
                    do_not_track = table.Column<string>(type: "text", nullable: false),
                    connection = table.Column<string>(type: "text", nullable: false),
                    cookie_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    on_line = table.Column<bool>(type: "boolean", nullable: false),
                    referrer = table.Column<string>(type: "text", nullable: false),
                    resolution = table.Column<string>(type: "text", nullable: false),
                    client_ip = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    extras = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_request_log", x => x.id);
                },
                comment: "Accesses to the website");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emails_to_admin");

            migrationBuilder.DropTable(
                name: "image_of_the_day");

            migrationBuilder.DropTable(
                name: "request_log");
        }
    }
}
