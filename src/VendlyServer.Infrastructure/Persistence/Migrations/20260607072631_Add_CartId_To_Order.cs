using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendlyServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_CartId_To_Order : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "cart_id",
                schema: "orders",
                table: "orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_cart_id",
                schema: "orders",
                table: "orders",
                column: "cart_id");

            migrationBuilder.AddForeignKey(
                name: "fk_orders_carts_cart_id",
                schema: "orders",
                table: "orders",
                column: "cart_id",
                principalSchema: "orders",
                principalTable: "carts",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_carts_cart_id",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_cart_id",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cart_id",
                schema: "orders",
                table: "orders");
        }
    }
}
