using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VendlyServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_CategoryPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category_prices",
                schema: "catalogs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    markup_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    rounding_step = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_category_prices_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalogs",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_category_prices_category_id",
                schema: "catalogs",
                table: "category_prices",
                column: "category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_prices",
                schema: "catalogs");
        }
    }
}
