using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchTextToAnimalsAndLots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable trigram extension for fuzzy search (requires CREATE privilege)
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                table: "Lots",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                table: "Animals",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            // Backfill Animals: normalize name + tagVisual + cuia
            // translate() strips accents, regexp_replace strips Spanish articles
            migrationBuilder.Sql("""
                UPDATE "Animals"
                SET "SearchText" = trim(regexp_replace(
                    translate(lower(
                        coalesce("Name", '') || ' ' ||
                        coalesce("TagVisual", '') || ' ' ||
                        coalesce("Cuia", '')
                    ), 'áéíóúüñÁÉÍÓÚÜÑ', 'aeioounAEIOOUN'),
                    '\y(la|el|los|las|un|una)\y', '', 'g'));
                """);

            // Backfill Lots: normalize name only
            migrationBuilder.Sql("""
                UPDATE "Lots"
                SET "SearchText" = trim(regexp_replace(
                    translate(lower(coalesce("Name", '')),
                        'áéíóúüñÁÉÍÓÚÜÑ', 'aeioounAEIOOUN'),
                    '\y(la|el|los|las|un|una)\y', '', 'g'));
                """);

            // GIN trigram indexes for fast ILIKE and similarity queries
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Animals_SearchText_trgm\" " +
                "ON \"Animals\" USING gin (\"SearchText\" gin_trgm_ops);"
            );
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Lots_SearchText_trgm\" " +
                "ON \"Lots\" USING gin (\"SearchText\" gin_trgm_ops);"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Animals_SearchText_trgm\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Lots_SearchText_trgm\";");

            migrationBuilder.DropColumn(
                name: "SearchText",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "SearchText",
                table: "Animals");
        }
    }
}
