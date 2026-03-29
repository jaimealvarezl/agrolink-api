using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropOwnerBrandRegistrationNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OwnerBrands_OwnerId_RegistrationNumber",
                table: "OwnerBrands");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "OwnerBrands");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "OwnerBrands",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnerBrands_OwnerId_RegistrationNumber",
                table: "OwnerBrands",
                columns: new[] { "OwnerId", "RegistrationNumber" },
                unique: true,
                filter: "\"RegistrationNumber\" IS NOT NULL");
        }
    }
}
