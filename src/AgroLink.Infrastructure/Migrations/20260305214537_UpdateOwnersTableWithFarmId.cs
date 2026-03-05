using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations;

/// <inheritdoc />
public partial class UpdateOwnersTableWithFarmId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Owners_Name",
            table: "Owners");

        migrationBuilder.DropIndex(
            name: "IX_Owners_UserId",
            table: "Owners");

        migrationBuilder.AddColumn<int>(
            name: "FarmId",
            table: "Owners",
            type: "integer",
            nullable: true);

        migrationBuilder.Sql(@"
                UPDATE ""Owners""
                SET ""FarmId"" = COALESCE(
                    (SELECT ""Id"" FROM ""Farms"" WHERE ""OwnerId"" = ""Owners"".""Id"" LIMIT 1),
                    (SELECT p.""FarmId"" 
                     FROM ""AnimalOwners"" ao
                     JOIN ""Animals"" a ON a.""Id"" = ao.""AnimalId""
                     JOIN ""Lots"" l ON l.""Id"" = a.""LotId""
                     JOIN ""Paddocks"" p ON p.""Id"" = l.""PaddockId""
                     WHERE ao.""OwnerId"" = ""Owners"".""Id"" LIMIT 1)
                     );
                     ");

                     migrationBuilder.CreateIndex(            name: "IX_Owners_FarmId",
            table: "Owners",
            column: "FarmId");

        migrationBuilder.CreateIndex(
            name: "IX_Owners_Name_FarmId",
            table: "Owners",
            columns: new[] { "Name", "FarmId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Owners_UserId",
            table: "Owners",
            column: "UserId");

        migrationBuilder.AddForeignKey(
            name: "FK_Owners_Farms_FarmId",
            table: "Owners",
            column: "FarmId",
            principalTable: "Farms",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Owners_Farms_FarmId",
            table: "Owners");

        migrationBuilder.DropIndex(
            name: "IX_Owners_FarmId",
            table: "Owners");

        migrationBuilder.DropIndex(
            name: "IX_Owners_Name_FarmId",
            table: "Owners");

        migrationBuilder.DropIndex(
            name: "IX_Owners_UserId",
            table: "Owners");

        migrationBuilder.DropColumn(
            name: "FarmId",
            table: "Owners");

        migrationBuilder.CreateIndex(
            name: "IX_Owners_Name",
            table: "Owners",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Owners_UserId",
            table: "Owners",
            column: "UserId",
            unique: true);
    }
}
