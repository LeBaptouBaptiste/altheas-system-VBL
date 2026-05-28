using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Althea_systems.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeFieldsToUserPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "user_payment_methods",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpMonth",
                table: "user_payment_methods",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpYear",
                table: "user_payment_methods",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Last4",
                table: "user_payment_methods",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentMethodId",
                table: "user_payment_methods",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_payment_methods_StripePaymentMethodId",
                table: "user_payment_methods",
                column: "StripePaymentMethodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_payment_methods_StripePaymentMethodId",
                table: "user_payment_methods");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "user_payment_methods");

            migrationBuilder.DropColumn(
                name: "ExpMonth",
                table: "user_payment_methods");

            migrationBuilder.DropColumn(
                name: "ExpYear",
                table: "user_payment_methods");

            migrationBuilder.DropColumn(
                name: "Last4",
                table: "user_payment_methods");

            migrationBuilder.DropColumn(
                name: "StripePaymentMethodId",
                table: "user_payment_methods");
        }
    }
}
