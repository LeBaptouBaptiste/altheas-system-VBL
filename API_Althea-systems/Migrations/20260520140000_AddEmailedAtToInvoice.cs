using API_Althea_systems.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Althea_systems.Migrations
{
    /// <summary>
    /// Phase 3 (email): adds Invoice.EmailedAt, the idempotency timestamp for
    /// the "order confirmed + PDF attached" email. NULL means "not yet sent",
    /// non-null means "we already sent it on this date" — Stripe webhook
    /// redelivery and the startup backfill both honor this to avoid spamming
    /// the customer.
    ///
    /// Hand-written, same pattern as 20260520120000_AddEmailConfirmationTokens.
    /// The model snapshot is intentionally NOT updated; if you regenerate
    /// migrations via the CLI later, accept the no-op follow-up migration.
    /// </summary>
    [DbContext(typeof(AltheaDbContext))]
    [Migration("20260520140000_AddEmailedAtToInvoice")]
    public class AddEmailedAtToInvoice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<System.DateTime>(
                name: "EmailedAt",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailedAt",
                table: "invoices");
        }
    }
}
