using API_Althea_systems.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Althea_systems.Migrations
{
    /// <summary>
    /// Phase 4b (email): adds User.TwoFactorMethod ("None" | "Authenticator" |
    /// "Email"). Stored as a string so future methods (SMS, WebAuthn, …)
    /// don't require an integer-shuffle migration.
    ///
    /// Backfill: every existing row gets "Authenticator" if TwoFactorEnabled,
    /// "None" otherwise. Preserves current behaviour for the seeded admin
    /// (who has TOTP set up) without any UX disruption.
    ///
    /// Same hand-written pattern as previous Phase migrations (model snapshot
    /// intentionally not updated; the next `dotnet ef migrations add` will
    /// regenerate a follow-up no-op which is safe to accept).
    /// </summary>
    [DbContext(typeof(AltheaDbContext))]
    [Migration("20260520160000_AddTwoFactorMethodToUser")]
    public class AddTwoFactorMethodToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TwoFactorMethod",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            // Backfill existing 2FA users — they're all on Authenticator
            // because that was the only option pre-phase-4b.
            migrationBuilder.Sql(
                @"UPDATE users
                  SET ""TwoFactorMethod"" = 'Authenticator'
                  WHERE ""TwoFactorEnabled"" = true;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFactorMethod",
                table: "users");
        }
    }
}
