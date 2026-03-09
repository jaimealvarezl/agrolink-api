using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMovementToAnimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movements_Animals_AnimalId",
                table: "Movements");

            migrationBuilder.DropForeignKey(
                name: "FK_Movements_Lots_LotId",
                table: "Movements");

            migrationBuilder.DropIndex(
                name: "IX_Movements_EntityType_EntityId",
                table: "Movements");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Movements");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "Movements");

            migrationBuilder.DropColumn(
                name: "FromId",
                table: "Movements");

            migrationBuilder.RenameColumn(
                name: "ToId",
                table: "Movements",
                newName: "ToLotId");

            migrationBuilder.RenameColumn(
                name: "LotId",
                table: "Movements",
                newName: "FromLotId");

            migrationBuilder.RenameIndex(
                name: "IX_Movements_LotId",
                table: "Movements",
                newName: "IX_Movements_FromLotId");

            migrationBuilder.AlterColumn<int>(
                name: "AnimalId",
                table: "Movements",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movements_ToLotId",
                table: "Movements",
                column: "ToLotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Movements_Animals_AnimalId",
                table: "Movements",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Movements_Lots_FromLotId",
                table: "Movements",
                column: "FromLotId",
                principalTable: "Lots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Movements_Lots_ToLotId",
                table: "Movements",
                column: "ToLotId",
                principalTable: "Lots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movements_Animals_AnimalId",
                table: "Movements");

            migrationBuilder.DropForeignKey(
                name: "FK_Movements_Lots_FromLotId",
                table: "Movements");

            migrationBuilder.DropForeignKey(
                name: "FK_Movements_Lots_ToLotId",
                table: "Movements");

            migrationBuilder.DropIndex(
                name: "IX_Movements_ToLotId",
                table: "Movements");

            migrationBuilder.RenameColumn(
                name: "ToLotId",
                table: "Movements",
                newName: "ToId");

            migrationBuilder.RenameColumn(
                name: "FromLotId",
                table: "Movements",
                newName: "LotId");

            migrationBuilder.RenameIndex(
                name: "IX_Movements_FromLotId",
                table: "Movements",
                newName: "IX_Movements_LotId");

            migrationBuilder.AlterColumn<int>(
                name: "AnimalId",
                table: "Movements",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "Movements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "Movements",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FromId",
                table: "Movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movements_EntityType_EntityId",
                table: "Movements",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Movements_Animals_AnimalId",
                table: "Movements",
                column: "AnimalId",
                principalTable: "Animals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Movements_Lots_LotId",
                table: "Movements",
                column: "LotId",
                principalTable: "Lots",
                principalColumn: "Id");
        }
    }
}
