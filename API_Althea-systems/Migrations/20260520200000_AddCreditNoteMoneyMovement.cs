using API_Althea_systems.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Althea_systems.Migrations
{
    /// <summary>
    /// Phase 7 — credit notes can now move actual money. The admin picks at
    /// issue time whether the credit goes back to the card (Stripe refund)
    /// or accumulates on the customer's account (store credit), and the
    /// customer can apply that store credit at the next checkout.
    ///
    /// Columns added:
    ///   - invoices.Mode (CreditNoteMode enum as string, nullable)
    ///   - invoices.StripeRefundId (string, nullable, indexed)
    ///   - invoices.RefundStatus (string, nullable)
    ///   - users.CreditBalanceCents (long, default 0)
    ///   - orders.CreditAppliedCents (long, default 0)
    /// </summary>
    [DbContext(typeof(AltheaDbContext))]
    [Migration("20260520200000_AddCreditNoteMoneyMovement")]
    public class AddCreditNoteMoneyMovement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Invoices
            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeRefundId",
                table: "invoices",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundStatus",
                table: "invoices",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_StripeRefundId",
                table: "invoices",
                column: "StripeRefundId");

            // Users
            migrationBuilder.AddColumn<long>(
                name: "CreditBalanceCents",
                table: "users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Orders
            migrationBuilder.AddColumn<long>(
                name: "CreditAppliedCents",
                table: "orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_invoices_StripeRefundId", table: "invoices");
            migrationBuilder.DropColumn(name: "Mode", table: "invoices");
            migrationBuilder.DropColumn(name: "StripeRefundId", table: "invoices");
            migrationBuilder.DropColumn(name: "RefundStatus", table: "invoices");
            migrationBuilder.DropColumn(name: "CreditBalanceCents", table: "users");
            migrationBuilder.DropColumn(name: "CreditAppliedCents", table: "orders");
        }
    }
}
