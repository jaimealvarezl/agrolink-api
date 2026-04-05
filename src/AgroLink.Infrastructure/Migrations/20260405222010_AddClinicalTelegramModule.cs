using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalTelegramModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicalCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FarmId = table.Column<int>(type: "integer", nullable: false),
                    AnimalId = table.Column<int>(type: "integer", nullable: true),
                    EarTag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FarmReferenceText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AnimalReferenceText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalCases_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalCases_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TechnicalSheet = table.Column<string>(type: "jsonb", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelegramInboundEventLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUpdateId = table.Column<long>(type: "bigint", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: true),
                    MessageId = table.Column<long>(type: "bigint", nullable: true),
                    RawPayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramInboundEventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClinicalCaseId = table.Column<int>(type: "integer", nullable: false),
                    AlertType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalAlerts_ClinicalCases_ClinicalCaseId",
                        column: x => x.ClinicalCaseId,
                        principalTable: "ClinicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalCaseEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClinicalCaseId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RawPayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    Transcript = table.Column<string>(type: "text", nullable: true),
                    StructuredDataJson = table.Column<string>(type: "jsonb", nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalCaseEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalCaseEvents_ClinicalCases_ClinicalCaseId",
                        column: x => x.ClinicalCaseId,
                        principalTable: "ClinicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClinicalCaseId = table.Column<int>(type: "integer", nullable: false),
                    RecommendationSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AdviceText = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Disclaimer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RawModelResponse = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalRecommendations_ClinicalCases_ClinicalCaseId",
                        column: x => x.ClinicalCaseId,
                        principalTable: "ClinicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelegramOutboundMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClinicalCaseId = table.Column<int>(type: "integer", nullable: true),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    MessageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TelegramMessageId = table.Column<long>(type: "bigint", nullable: true),
                    DeliveryStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramOutboundMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramOutboundMessages_ClinicalCases_ClinicalCaseId",
                        column: x => x.ClinicalCaseId,
                        principalTable: "ClinicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MedicationImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedicationId = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationImages_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedicationId = table.Column<int>(type: "integer", nullable: false),
                    Species = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SymptomTags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WeightMin = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    WeightMax = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    DoseFormula = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Contraindications = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationRules_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalAlerts_ClinicalCaseId_CreatedAt",
                table: "ClinicalAlerts",
                columns: new[] { "ClinicalCaseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCaseEvents_ClinicalCaseId_CreatedAt",
                table: "ClinicalCaseEvents",
                columns: new[] { "ClinicalCaseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCases_AnimalId",
                table: "ClinicalCases",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCases_FarmId_AnimalId_OpenedAt",
                table: "ClinicalCases",
                columns: new[] { "FarmId", "AnimalId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCases_FarmId_EarTag_OpenedAt",
                table: "ClinicalCases",
                columns: new[] { "FarmId", "EarTag", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalRecommendations_ClinicalCaseId_CreatedAt",
                table: "ClinicalRecommendations",
                columns: new[] { "ClinicalCaseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationImages_MedicationId",
                table: "MedicationImages",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRules_MedicationId",
                table: "MedicationRules",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRules_Species_Active",
                table: "MedicationRules",
                columns: new[] { "Species", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Active",
                table: "Medications",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Name",
                table: "Medications",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramInboundEventLogs_ChatId_CreatedAt",
                table: "TelegramInboundEventLogs",
                columns: new[] { "ChatId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramInboundEventLogs_TelegramUpdateId",
                table: "TelegramInboundEventLogs",
                column: "TelegramUpdateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOutboundMessages_ChatId_CreatedAt",
                table: "TelegramOutboundMessages",
                columns: new[] { "ChatId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOutboundMessages_ClinicalCaseId",
                table: "TelegramOutboundMessages",
                column: "ClinicalCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOutboundMessages_IdempotencyKey",
                table: "TelegramOutboundMessages",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicalAlerts");

            migrationBuilder.DropTable(
                name: "ClinicalCaseEvents");

            migrationBuilder.DropTable(
                name: "ClinicalRecommendations");

            migrationBuilder.DropTable(
                name: "MedicationImages");

            migrationBuilder.DropTable(
                name: "MedicationRules");

            migrationBuilder.DropTable(
                name: "TelegramInboundEventLogs");

            migrationBuilder.DropTable(
                name: "TelegramOutboundMessages");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "ClinicalCases");
        }
    }
}
