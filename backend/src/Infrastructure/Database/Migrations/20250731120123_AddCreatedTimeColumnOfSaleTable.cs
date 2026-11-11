using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedTimeColumnOfSaleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_purchase_product_entry_products_product_id",
                schema: "public",
                table: "purchase_product_entry");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_product_entry_purchases_purchase_id",
                schema: "public",
                table: "purchase_product_entry");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_product_entry_products_product_id",
                schema: "public",
                table: "sale_product_entry");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_product_entry_sales_sale_id",
                schema: "public",
                table: "sale_product_entry");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_time",
                schema: "public",
                table: "sale",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_product_entry_product_product_id",
                schema: "public",
                table: "purchase_product_entry",
                column: "product_id",
                principalSchema: "public",
                principalTable: "product",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_product_entry_purchase_purchase_id",
                schema: "public",
                table: "purchase_product_entry",
                column: "purchase_id",
                principalSchema: "public",
                principalTable: "purchase",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_product_entry_product_product_id",
                schema: "public",
                table: "sale_product_entry",
                column: "product_id",
                principalSchema: "public",
                principalTable: "product",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_product_entry_sale_sale_id",
                schema: "public",
                table: "sale_product_entry",
                column: "sale_id",
                principalSchema: "public",
                principalTable: "sale",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_purchase_product_entry_product_product_id",
                schema: "public",
                table: "purchase_product_entry");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_product_entry_purchase_purchase_id",
                schema: "public",
                table: "purchase_product_entry");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_product_entry_product_product_id",
                schema: "public",
                table: "sale_product_entry");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_product_entry_sale_sale_id",
                schema: "public",
                table: "sale_product_entry");

            migrationBuilder.DropColumn(
                name: "created_time",
                schema: "public",
                table: "sale");

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_product_entry_products_product_id",
                schema: "public",
                table: "purchase_product_entry",
                column: "product_id",
                principalSchema: "public",
                principalTable: "product",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_product_entry_purchases_purchase_id",
                schema: "public",
                table: "purchase_product_entry",
                column: "purchase_id",
                principalSchema: "public",
                principalTable: "purchase",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_product_entry_products_product_id",
                schema: "public",
                table: "sale_product_entry",
                column: "product_id",
                principalSchema: "public",
                principalTable: "product",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_product_entry_sales_sale_id",
                schema: "public",
                table: "sale_product_entry",
                column: "sale_id",
                principalSchema: "public",
                principalTable: "sale",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
