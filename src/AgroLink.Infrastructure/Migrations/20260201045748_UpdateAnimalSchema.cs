using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnimalSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Animals_Tag",
                table: "Animals");

            migrationBuilder.RenameColumn(
                name: "Tag",
                table: "Animals",
                newName: "TagVisual");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Animals",
                newName: "ReproductiveStatus");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BirthDate",
                table: "Animals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cuia",
                table: "Animals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthStatus",
                table: "Animals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LifeStatus",
                table: "Animals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductionStatus",
                table: "Animals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cuia",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "LifeStatus",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "ProductionStatus",
                table: "Animals");

            migrationBuilder.RenameColumn(
                name: "TagVisual",
                table: "Animals",
                newName: "Tag");

            migrationBuilder.RenameColumn(
                name: "ReproductiveStatus",
                table: "Animals",
                newName: "Status");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BirthDate",
                table: "Animals",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_Tag",
                table: "Animals",
                column: "Tag",
                unique: true);
        }
    }
}
