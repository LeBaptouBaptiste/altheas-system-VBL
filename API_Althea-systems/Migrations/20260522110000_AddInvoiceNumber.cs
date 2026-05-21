using API_Althea_systems.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Althea_systems.Migrations
{
    /// <summary>
    /// Adds <c>invoices.Number</c> — the human-readable invoice number in the
    /// VBL-mandated format <c>{ClientCode}-{YYYY}-{MM}-{NNNN}</c> (client code
    /// = first 8 hex chars of User.Id, monthly sequence per customer).
    ///
    /// Backfill strategy: assign numbers to every existing invoice in one SQL
    /// pass using <c>ROW_NUMBER() OVER (PARTITION BY user, year, month)</c>.
    /// Old rows get the same format as new ones — the admin UI can drop the
    /// GUID fallback once this migration ships.
    ///
    /// Column is added nullable, backfilled, then promoted to NOT NULL with a
    /// unique index. Three-step pattern is safe because the table is small
    /// (transactional, not analytical) and the backfill is deterministic.
    /// </summary>
    [DbContext(typeof(AltheaDbContext))]
    [Migration("20260522110000_AddInvoiceNumber")]
    public class AddInvoiceNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add column as nullable so existing rows survive the migration
            //    before backfill.
            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "invoices",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            // 2. Backfill: window-function-based per-(user, year, month) sequence.
            //    The join via Order is needed because Invoice doesn't directly
            //    carry UserId — it lives on the parent Order.
            //
            //    Format: {first-8-hex-of-userid-upper}-{YYYY}-{MM}-{4-digit-seq}
            //    Order by Date then Id to make the assignment deterministic
            //    (two invoices issued in the same millisecond would otherwise
            //    flip on each migration run).
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT
                        i.""Id"" AS invoice_id,
                        UPPER(SUBSTRING(REPLACE(o.""UserId""::text, '-', '') FROM 1 FOR 8))
                            || '-' || TO_CHAR(i.""Date"", 'YYYY')
                            || '-' || TO_CHAR(i.""Date"", 'MM')
                            || '-' || LPAD(
                                ROW_NUMBER() OVER (
                                    PARTITION BY
                                        o.""UserId"",
                                        EXTRACT(YEAR FROM i.""Date""),
                                        EXTRACT(MONTH FROM i.""Date"")
                                    ORDER BY i.""Date"", i.""Id""
                                )::text,
                                4, '0'
                            ) AS computed_number
                    FROM invoices i
                    JOIN orders o ON o.""Id"" = i.""OrderId""
                )
                UPDATE invoices
                SET ""Number"" = numbered.computed_number
                FROM numbered
                WHERE invoices.""Id"" = numbered.invoice_id;
            ");

            // 3. Now that every row has a value, promote to NOT NULL + add the
            //    unique index. Safe because the backfill above is collision-free
            //    by construction (per-period sequence is unique within a period
            //    and the period is part of the key).
            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "invoices",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_Number",
                table: "invoices",
                column: "Number",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_invoices_Number", table: "invoices");
            migrationBuilder.DropColumn(name: "Number", table: "invoices");
        }
    }
}
