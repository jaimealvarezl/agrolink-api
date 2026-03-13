using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceChecklistScopeWithLotId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Checklists_ScopeType_ScopeId_Date",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                table: "Checklists");

            migrationBuilder.RenameColumn(
                name: "ScopeId",
                table: "Checklists",
                newName: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_LotId_Date",
                table: "Checklists",
                columns: new[] { "LotId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_Lots_LotId",
                table: "Checklists",
                column: "LotId",
                principalTable: "Lots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_Lots_LotId",
                table: "Checklists");

            migrationBuilder.DropIndex(
                name: "IX_Checklists_LotId_Date",
                table: "Checklists");

            migrationBuilder.RenameColumn(
                name: "LotId",
                table: "Checklists",
                newName: "ScopeId");

            migrationBuilder.AddColumn<string>(
                name: "ScopeType",
                table: "Checklists",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_ScopeType_ScopeId_Date",
                table: "Checklists",
                columns: new[] { "ScopeType", "ScopeId", "Date" });
        }
    }
}
