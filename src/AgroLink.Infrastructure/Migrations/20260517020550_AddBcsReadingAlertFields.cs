using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBcsReadingAlertFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlertDescription",
                table: "AnimalBcsReadings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAlerts",
                table: "AnimalBcsReadings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertDescription",
                table: "AnimalBcsReadings");

            migrationBuilder.DropColumn(
                name: "HasAlerts",
                table: "AnimalBcsReadings");
        }
    }
}
