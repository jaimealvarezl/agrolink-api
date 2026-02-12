using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnimalPhotoWithStorageKeyAndUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnimalPhotos_AnimalId_IsProfile",
                table: "AnimalPhotos");

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "AnimalPhotos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalPhotos_AnimalId_IsProfile",
                table: "AnimalPhotos",
                columns: new[] { "AnimalId", "IsProfile" },
                unique: true,
                filter: "\"IsProfile\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnimalPhotos_AnimalId_IsProfile",
                table: "AnimalPhotos");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "AnimalPhotos");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalPhotos_AnimalId_IsProfile",
                table: "AnimalPhotos",
                columns: new[] { "AnimalId", "IsProfile" },
                filter: "\"IsProfile\" = true");
        }
    }
}
