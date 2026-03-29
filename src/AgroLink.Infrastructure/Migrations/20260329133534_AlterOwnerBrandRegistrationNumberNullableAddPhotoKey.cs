using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlterOwnerBrandRegistrationNumberNullableAddPhotoKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OwnerBrands_OwnerId_RegistrationNumber",
                table: "OwnerBrands");

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationNumber",
                table: "OwnerBrands",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "PhotoStorageKey",
                table: "OwnerBrands",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnerBrands_OwnerId_RegistrationNumber",
                table: "OwnerBrands",
                columns: new[] { "OwnerId", "RegistrationNumber" },
                unique: true,
                filter: "\"RegistrationNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OwnerBrands_OwnerId_RegistrationNumber",
                table: "OwnerBrands");

            migrationBuilder.DropColumn(
                name: "PhotoStorageKey",
                table: "OwnerBrands");

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationNumber",
                table: "OwnerBrands",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnerBrands_OwnerId_RegistrationNumber",
                table: "OwnerBrands",
                columns: new[] { "OwnerId", "RegistrationNumber" },
                unique: true);
        }
    }
}
