using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmMemberAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FarmMember_Farms_FarmId",
                table: "FarmMember");

            migrationBuilder.DropForeignKey(
                name: "FK_FarmMember_Users_UserId",
                table: "FarmMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FarmMember",
                table: "FarmMember");

            migrationBuilder.RenameTable(
                name: "FarmMember",
                newName: "FarmMembers");

            migrationBuilder.RenameIndex(
                name: "IX_FarmMember_UserId",
                table: "FarmMembers",
                newName: "IX_FarmMembers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FarmMember_FarmId",
                table: "FarmMembers",
                newName: "IX_FarmMembers_FarmId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FarmMembers",
                table: "FarmMembers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FarmMembers_Farms_FarmId",
                table: "FarmMembers",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FarmMembers_Users_UserId",
                table: "FarmMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FarmMembers_Farms_FarmId",
                table: "FarmMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_FarmMembers_Users_UserId",
                table: "FarmMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FarmMembers",
                table: "FarmMembers");

            migrationBuilder.RenameTable(
                name: "FarmMembers",
                newName: "FarmMember");

            migrationBuilder.RenameIndex(
                name: "IX_FarmMembers_UserId",
                table: "FarmMember",
                newName: "IX_FarmMember_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FarmMembers_FarmId",
                table: "FarmMember",
                newName: "IX_FarmMember_FarmId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FarmMember",
                table: "FarmMember",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FarmMember_Farms_FarmId",
                table: "FarmMember",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FarmMember_Users_UserId",
                table: "FarmMember",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
