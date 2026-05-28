using API_Althea_systems.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Althea_systems.Migrations
{
    /// <summary>
    /// Adds Malay / Arabic translation columns to <c>products</c> (Name,
    /// Description, LongDescription) and <c>product_specs</c> (Label, Value
    /// per locale: EN / MS / AR).
    ///
    /// All new columns are nullable on purpose — the front falls back to the
    /// French canonical value when a locale isn't filled in, so existing
    /// rows keep working transparently after deploy.
    /// </summary>
    [DbContext(typeof(AltheaDbContext))]
    [Migration("20260522080000_AddMsArTranslationsToProducts")]
    public class AddMsArTranslationsToProducts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── products ─────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "NameMs",
                table: "products",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "products",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionMs",
                table: "products",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "products",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LongDescriptionMs",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LongDescriptionAr",
                table: "products",
                type: "text",
                nullable: true);

            // ── product_specs ────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "LabelEn",
                table: "product_specs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabelMs",
                table: "product_specs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabelAr",
                table: "product_specs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValueEn",
                table: "product_specs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValueMs",
                table: "product_specs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValueAr",
                table: "product_specs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "NameMs", table: "products");
            migrationBuilder.DropColumn(name: "NameAr", table: "products");
            migrationBuilder.DropColumn(name: "DescriptionMs", table: "products");
            migrationBuilder.DropColumn(name: "DescriptionAr", table: "products");
            migrationBuilder.DropColumn(name: "LongDescriptionMs", table: "products");
            migrationBuilder.DropColumn(name: "LongDescriptionAr", table: "products");
            migrationBuilder.DropColumn(name: "LabelEn", table: "product_specs");
            migrationBuilder.DropColumn(name: "LabelMs", table: "product_specs");
            migrationBuilder.DropColumn(name: "LabelAr", table: "product_specs");
            migrationBuilder.DropColumn(name: "ValueEn", table: "product_specs");
            migrationBuilder.DropColumn(name: "ValueMs", table: "product_specs");
            migrationBuilder.DropColumn(name: "ValueAr", table: "product_specs");
        }
    }
}
