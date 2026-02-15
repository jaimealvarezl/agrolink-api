using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForBreedAndColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Animals_Breed",
                table: "Animals",
                column: "Breed");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_Color",
                table: "Animals",
                column: "Color");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Animals_Breed",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_Color",
                table: "Animals");
        }
    }
}
