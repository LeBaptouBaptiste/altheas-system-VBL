using API_Althea_systems.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Althea_systems.Migrations
{
    /// <summary>
    /// Adds Address.IsDefault + Address.Archived to back the Amazon-style
    /// address picker. Hand-written, same pattern as previous Phase migrations.
    ///
    /// One-time backfill: for every user that has at least one non-default
    /// address, promote the OLDEST (first one they ever created) to default.
    /// Without this, existing accounts would show "no default" on first load
    /// of /account/addresses post-deploy.
    /// </summary>
    [DbContext(typeof(AltheaDbContext))]
    [Migration("20260521080000_AddDefaultAndArchivedToAddress")]
    public class AddDefaultAndArchivedToAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Archived",
                table: "addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: for each user that has ≥ 1 address, mark the oldest
            // as default. Uses DISTINCT ON (PG-specific) — cheapest way to
            // pick "first per group" without a window function pipeline.
            migrationBuilder.Sql(@"
                UPDATE addresses
                SET ""IsDefault"" = true
                WHERE ""Id"" IN (
                    SELECT DISTINCT ON (""UserId"") ""Id""
                    FROM addresses
                    ORDER BY ""UserId"", ""CreatedAt"" ASC
                );");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsDefault", table: "addresses");
            migrationBuilder.DropColumn(name: "Archived", table: "addresses");
        }
    }
}
