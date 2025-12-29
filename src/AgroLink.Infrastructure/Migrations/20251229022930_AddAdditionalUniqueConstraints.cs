using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FarmMembers_FarmId",
                table: "FarmMembers");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistItems_ChecklistId",
                table: "ChecklistItems");

            migrationBuilder.CreateIndex(
                name: "IX_FarmMembers_FarmId_UserId",
                table: "FarmMembers",
                columns: new[] { "FarmId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_ChecklistId_AnimalId",
                table: "ChecklistItems",
                columns: new[] { "ChecklistId", "AnimalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FarmMembers_FarmId_UserId",
                table: "FarmMembers");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistItems_ChecklistId_AnimalId",
                table: "ChecklistItems");

            migrationBuilder.CreateIndex(
                name: "IX_FarmMembers_FarmId",
                table: "FarmMembers",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_ChecklistId",
                table: "ChecklistItems",
                column: "ChecklistId");
        }
    }
}
