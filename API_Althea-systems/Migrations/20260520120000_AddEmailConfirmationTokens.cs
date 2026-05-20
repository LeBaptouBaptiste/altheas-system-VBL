using API_Althea_systems.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Althea_systems.Migrations
{
    /// <summary>
    /// Phase 2 (email): single-use confirmation tokens issued at registration.
    /// We store the SHA-256 hash (hex, 64 chars), never the raw token —
    /// a DB dump can't be used to confirm arbitrary accounts.
    ///
    /// Hand-written migration (no <c>dotnet ef migrations add</c> snapshot
    /// update): the modelSnapshot keeps its pre-Phase-2 state, which means
    /// the next <c>migrations add</c> run will re-diff against the live model
    /// and regenerate this table in its own follow-up migration. Just accept
    /// the re-generated migration as a no-op (the table already exists; EF
    /// will skip it on apply because the migrations history table records
    /// this one).
    /// </summary>
    // Both attributes are required for the EF Migrations machinery to pick the
    // class up: [DbContext] tells it which context owns the migration, and
    // [Migration] supplies the sortable ID that ends up in the
    // __EFMigrationsHistory table.
    [DbContext(typeof(AltheaDbContext))]
    [Migration("20260520120000_AddEmailConfirmationTokens")]
    public class AddEmailConfirmationTokens : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_confirmation_tokens",
                columns: table => new
                {
                    Id = table.Column<System.Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<System.Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<System.DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<System.DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<System.DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_confirmation_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_confirmation_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_confirmation_tokens_TokenHash",
                table: "email_confirmation_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_confirmation_tokens_UserId",
                table: "email_confirmation_tokens",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_confirmation_tokens");
        }
    }
}
