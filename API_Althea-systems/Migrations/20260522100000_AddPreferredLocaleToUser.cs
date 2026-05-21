using API_Althea_systems.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Althea_systems.Migrations
{
    /// <summary>
    /// Adds <c>users.PreferredLocale</c> (nullable 2-letter code: fr/en/ms/ar).
    /// Powers locale-aware transactional emails via the
    /// <see cref="API_Althea_systems.Services.Email.EmailTemplateRenderer"/>
    /// language-suffix lookup (<c>welcome.en.html</c> etc.).
    /// </summary>
    [DbContext(typeof(AltheaDbContext))]
    [Migration("20260522100000_AddPreferredLocaleToUser")]
    public class AddPreferredLocaleToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredLocale",
                table: "users",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PreferredLocale", table: "users");
        }
    }
}
