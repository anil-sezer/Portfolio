using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRequestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "device_type",
                table: "request",
                newName: "country");

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "request",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "city",
                table: "request");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "request",
                newName: "device_type");
        }
    }
}
