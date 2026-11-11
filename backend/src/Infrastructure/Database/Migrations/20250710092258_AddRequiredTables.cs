using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiredTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "brands",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_tags",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    normalized_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_units",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    normalized_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchase",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    occurence_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_tags",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    normalized_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    normalized_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    occurence_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_tags",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    normalized_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_price",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_from_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_price", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_price_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "public",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_product_tag",
                schema: "public",
                columns: table => new
                {
                    products_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tags_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_product_tag", x => new { x.products_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_product_product_tag_product_tags_tags_id",
                        column: x => x.tags_id,
                        principalSchema: "public",
                        principalTable: "product_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_product_tag_products_products_id",
                        column: x => x.products_id,
                        principalSchema: "public",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_detail",
                schema: "public",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    brand_id = table.Column<int>(type: "integer", nullable: false),
                    measure = table.Column<float>(type: "real", nullable: false),
                    measure_unit_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_detail", x => x.product_id);
                    table.ForeignKey(
                        name: "fk_product_detail_brands_brand_id",
                        column: x => x.brand_id,
                        principalSchema: "public",
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_detail_product_units_measure_unit_id",
                        column: x => x.measure_unit_id,
                        principalSchema: "public",
                        principalTable: "product_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_detail_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "public",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_purchase_tag",
                schema: "public",
                columns: table => new
                {
                    purchases_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tags_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_purchase_tag", x => new { x.purchases_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_purchase_purchase_tag_purchase_tags_tags_id",
                        column: x => x.tags_id,
                        principalSchema: "public",
                        principalTable: "purchase_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_purchase_purchase_tag_purchases_purchases_id",
                        column: x => x.purchases_id,
                        principalSchema: "public",
                        principalTable: "purchase",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sale_sale_tag",
                schema: "public",
                columns: table => new
                {
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tags_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_sale_tag", x => new { x.sale_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_sale_sale_tag_sale_tags_tags_id",
                        column: x => x.tags_id,
                        principalSchema: "public",
                        principalTable: "sale_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sale_sale_tag_sales_sale_id",
                        column: x => x.sale_id,
                        principalSchema: "public",
                        principalTable: "sale",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "public",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_product_entry",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_product_entry", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_product_entry_product_price_price_id",
                        column: x => x.price_id,
                        principalSchema: "public",
                        principalTable: "product_price",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_product_entry_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "public",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_product_entry_purchases_purchase_id",
                        column: x => x.purchase_id,
                        principalSchema: "public",
                        principalTable: "purchase",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sale_product_entry",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_product_entry", x => x.id);
                    table.ForeignKey(
                        name: "fk_sale_product_entry_product_price_price_id",
                        column: x => x.price_id,
                        principalSchema: "public",
                        principalTable: "product_price",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_product_entry_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "public",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_product_entry_sales_sale_id",
                        column: x => x.sale_id,
                        principalSchema: "public",
                        principalTable: "sale",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_detail_brand_id",
                schema: "public",
                table: "product_detail",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_detail_measure_unit_id",
                schema: "public",
                table: "product_detail",
                column: "measure_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_price_product_id",
                schema: "public",
                table: "product_price",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_product_tag_tags_id",
                schema: "public",
                table: "product_product_tag",
                column: "tags_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_tags_normalized_name",
                schema: "public",
                table: "product_tags",
                column: "normalized_name");

            migrationBuilder.CreateIndex(
                name: "ix_product_units_normalized_name",
                schema: "public",
                table: "product_units",
                column: "normalized_name");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_product_entry_price_id",
                schema: "public",
                table: "purchase_product_entry",
                column: "price_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_product_entry_product_id",
                schema: "public",
                table: "purchase_product_entry",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_product_entry_purchase_id",
                schema: "public",
                table: "purchase_product_entry",
                column: "purchase_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_purchase_tag_tags_id",
                schema: "public",
                table: "purchase_purchase_tag",
                column: "tags_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_tags_normalized_name",
                schema: "public",
                table: "purchase_tags",
                column: "normalized_name");

            migrationBuilder.CreateIndex(
                name: "ix_role_normalized_name",
                schema: "public",
                table: "role",
                column: "normalized_name");

            migrationBuilder.CreateIndex(
                name: "ix_sale_product_entry_price_id",
                schema: "public",
                table: "sale_product_entry",
                column: "price_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_product_entry_product_id",
                schema: "public",
                table: "sale_product_entry",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_product_entry_sale_id",
                schema: "public",
                table: "sale_product_entry",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_sale_tag_tags_id",
                schema: "public",
                table: "sale_sale_tag",
                column: "tags_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_tags_normalized_name",
                schema: "public",
                table: "sale_tags",
                column: "normalized_name");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "public",
                table: "user_roles",
                column: "role_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id",
                schema: "public",
                table: "user_roles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_detail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_product_tag",
                schema: "public");

            migrationBuilder.DropTable(
                name: "purchase_product_entry",
                schema: "public");

            migrationBuilder.DropTable(
                name: "purchase_purchase_tag",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sale_product_entry",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sale_sale_tag",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "brands",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_units",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_tags",
                schema: "public");

            migrationBuilder.DropTable(
                name: "purchase_tags",
                schema: "public");

            migrationBuilder.DropTable(
                name: "purchase",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_price",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sale_tags",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sale",
                schema: "public");

            migrationBuilder.DropTable(
                name: "role",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product",
                schema: "public");
        }
    }
}
