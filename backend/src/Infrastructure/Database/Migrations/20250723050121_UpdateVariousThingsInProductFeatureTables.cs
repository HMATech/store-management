using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVariousThingsInProductFeatureTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_detail_products_product_id",
                schema: "public",
                table: "product_detail");

            migrationBuilder.DropForeignKey(
                name: "fk_product_price_products_product_id",
                schema: "public",
                table: "product_price");

            migrationBuilder.DropForeignKey(
                name: "fk_product_detail_product_units_measure_unit_id",
                schema: "public",
                table: "product_detail");

            migrationBuilder.DropForeignKey(
                name: "fk_product_product_tag_product_tags_tags_id",
                schema: "public",
                table: "product_product_tag");

            migrationBuilder.DropPrimaryKey(
                name: "pk_product_units",
                schema: "public",
                table: "product_units");

            migrationBuilder.DropPrimaryKey(
                name: "pk_product_tags",
                schema: "public",
                table: "product_tags");

            migrationBuilder.RenameTable(
                name: "product_units",
                schema: "public",
                newName: "product_unit",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "product_tags",
                schema: "public",
                newName: "product_tag",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "ix_product_units_normalized_name",
                schema: "public",
                table: "product_unit",
                newName: "ix_product_unit_normalized_name");

            migrationBuilder.RenameIndex(
                name: "ix_product_tags_normalized_name",
                schema: "public",
                table: "product_tag",
                newName: "ix_product_tag_normalized_name");

            migrationBuilder.AlterColumn<DateTime>(
                name: "valid_to_time",
                schema: "public",
                table: "product_price",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "pk_product_unit",
                schema: "public",
                table: "product_unit",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_product_tag",
                schema: "public",
                table: "product_tag",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_product_detail_product_product_id",
                schema: "public",
                table: "product_detail",
                column: "product_id",
                principalSchema: "public",
                principalTable: "product",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_product_price_product_product_id",
                schema: "public",
                table: "product_price",
                column: "product_id",
                principalSchema: "public",
                principalTable: "product",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_product_detail_product_unit_measure_unit_id",
                schema: "public",
                table: "product_detail",
                column: "measure_unit_id",
                principalSchema: "public",
                principalTable: "product_unit",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_product_tag_product_tag_tags_id",
                schema: "public",
                table: "product_product_tag",
                column: "tags_id",
                principalSchema: "public",
                principalTable: "product_tag",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_detail_product_product_id",
                schema: "public",
                table: "product_detail");

            migrationBuilder.DropForeignKey(
                name: "fk_product_price_product_product_id",
                schema: "public",
                table: "product_price");

            migrationBuilder.DropForeignKey(
                name: "fk_product_detail_product_unit_measure_unit_id",
                schema: "public",
                table: "product_detail");

            migrationBuilder.DropForeignKey(
                name: "fk_product_product_tag_product_tag_tags_id",
                schema: "public",
                table: "product_product_tag");

            migrationBuilder.DropPrimaryKey(
                name: "pk_product_unit",
                schema: "public",
                table: "product_unit");

            migrationBuilder.DropPrimaryKey(
                name: "pk_product_tag",
                schema: "public",
                table: "product_tag");

            migrationBuilder.RenameTable(
                name: "product_unit",
                schema: "public",
                newName: "product_units",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "product_tag",
                schema: "public",
                newName: "product_tags",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "ix_product_unit_normalized_name",
                schema: "public",
                table: "product_units",
                newName: "ix_product_units_normalized_name");

            migrationBuilder.RenameIndex(
                name: "ix_product_tag_normalized_name",
                schema: "public",
                table: "product_tags",
                newName: "ix_product_tags_normalized_name");

            migrationBuilder.AlterColumn<DateTime>(
                name: "valid_to_time",
                schema: "public",
                table: "product_price",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_product_units",
                schema: "public",
                table: "product_units",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_product_tags",
                schema: "public",
                table: "product_tags",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_product_detail_products_product_id",
                schema: "public",
                table: "product_detail",
                column: "product_id",
                principalSchema: "public",
                principalTable: "product",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_product_price_products_product_id",
                schema: "public",
                table: "product_price",
                column: "product_id",
                principalSchema: "public",
                principalTable: "product",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_product_detail_product_units_measure_unit_id",
                schema: "public",
                table: "product_detail",
                column: "measure_unit_id",
                principalSchema: "public",
                principalTable: "product_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            
            migrationBuilder.AddForeignKey(
                name: "fk_product_product_tag_product_tags_tags_id",
                schema: "public",
                table: "product_product_tag",
                column: "tags_id",
                principalSchema: "public",
                principalTable: "product_tags",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
