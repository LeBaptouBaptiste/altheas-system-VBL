using API_Althea_systems.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Althea_systems.Migrations
{
    /// <summary>
    /// Extends MS / AR translation columns to <c>categories</c>,
    /// <c>hero_slides</c>, and <c>static_pages</c> — same nullable pattern as
    /// the Product migration. Front falls back to FR for unset locales.
    /// </summary>
    [DbContext(typeof(AltheaDbContext))]
    [Migration("20260522090000_AddMsArTranslationsToContent")]
    public class AddMsArTranslationsToContent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── categories ───────────────────────────────
            migrationBuilder.AddColumn<string>(name: "NameMs", table: "categories",
                type: "character varying(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(name: "NameAr", table: "categories",
                type: "character varying(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(name: "DescriptionMs", table: "categories",
                type: "character varying(1000)", maxLength: 1000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "DescriptionAr", table: "categories",
                type: "character varying(1000)", maxLength: 1000, nullable: true);

            // ── hero_slides ──────────────────────────────
            migrationBuilder.AddColumn<string>(name: "TitleMs", table: "hero_slides",
                type: "character varying(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(name: "TitleAr", table: "hero_slides",
                type: "character varying(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(name: "SubtitleMs", table: "hero_slides",
                type: "character varying(300)", maxLength: 300, nullable: true);
            migrationBuilder.AddColumn<string>(name: "SubtitleAr", table: "hero_slides",
                type: "character varying(300)", maxLength: 300, nullable: true);
            migrationBuilder.AddColumn<string>(name: "DescriptionMs", table: "hero_slides",
                type: "character varying(1000)", maxLength: 1000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "DescriptionAr", table: "hero_slides",
                type: "character varying(1000)", maxLength: 1000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "CtaMs", table: "hero_slides",
                type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "CtaAr", table: "hero_slides",
                type: "character varying(100)", maxLength: 100, nullable: true);

            // ── static_pages ─────────────────────────────
            migrationBuilder.AddColumn<string>(name: "TitleMs", table: "static_pages",
                type: "character varying(300)", maxLength: 300, nullable: true);
            migrationBuilder.AddColumn<string>(name: "TitleAr", table: "static_pages",
                type: "character varying(300)", maxLength: 300, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ContentMs", table: "static_pages",
                type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ContentAr", table: "static_pages",
                type: "text", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "NameMs", table: "categories");
            migrationBuilder.DropColumn(name: "NameAr", table: "categories");
            migrationBuilder.DropColumn(name: "DescriptionMs", table: "categories");
            migrationBuilder.DropColumn(name: "DescriptionAr", table: "categories");
            migrationBuilder.DropColumn(name: "TitleMs", table: "hero_slides");
            migrationBuilder.DropColumn(name: "TitleAr", table: "hero_slides");
            migrationBuilder.DropColumn(name: "SubtitleMs", table: "hero_slides");
            migrationBuilder.DropColumn(name: "SubtitleAr", table: "hero_slides");
            migrationBuilder.DropColumn(name: "DescriptionMs", table: "hero_slides");
            migrationBuilder.DropColumn(name: "DescriptionAr", table: "hero_slides");
            migrationBuilder.DropColumn(name: "CtaMs", table: "hero_slides");
            migrationBuilder.DropColumn(name: "CtaAr", table: "hero_slides");
            migrationBuilder.DropColumn(name: "TitleMs", table: "static_pages");
            migrationBuilder.DropColumn(name: "TitleAr", table: "static_pages");
            migrationBuilder.DropColumn(name: "ContentMs", table: "static_pages");
            migrationBuilder.DropColumn(name: "ContentAr", table: "static_pages");
        }
    }
}
