using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RecordUnitCostOnPurchaseEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                schema: "public",
                table: "purchase_product_entry",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            // Purchases used to be valued at the linked selling price, so that is the best known cost.
            migrationBuilder.Sql(
                """
                UPDATE public.purchase_product_entry AS e
                SET unit_cost = p.value
                FROM public.product_price AS p
                WHERE p.id = e.price_id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_product_entry_product_price_price_id",
                schema: "public",
                table: "purchase_product_entry");

            migrationBuilder.DropIndex(
                name: "ix_purchase_product_entry_price_id",
                schema: "public",
                table: "purchase_product_entry");

            migrationBuilder.DropColumn(
                name: "price_id",
                schema: "public",
                table: "purchase_product_entry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "price_id",
                schema: "public",
                table: "purchase_product_entry",
                type: "uuid",
                nullable: true);

            // The cost can't be turned back into a price line, so link each line to its product's latest price.
            migrationBuilder.Sql(
                """
                UPDATE public.purchase_product_entry AS e
                SET price_id = (
                    SELECT p.id FROM public.product_price AS p
                    WHERE p.product_id = e.product_id
                    ORDER BY p.valid_to_time IS NOT NULL, p.valid_from_time DESC
                    LIMIT 1);
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "price_id",
                schema: "public",
                table: "purchase_product_entry",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "unit_cost",
                schema: "public",
                table: "purchase_product_entry");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_product_entry_price_id",
                schema: "public",
                table: "purchase_product_entry",
                column: "price_id");

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_product_entry_product_price_price_id",
                schema: "public",
                table: "purchase_product_entry",
                column: "price_id",
                principalSchema: "public",
                principalTable: "product_price",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
