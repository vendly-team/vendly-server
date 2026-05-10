using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VendlyServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LoggingMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cart_items_products_product_id",
                schema: "orders",
                table: "cart_items");

            migrationBuilder.DropForeignKey(
                name: "fk_carts_users_user_id",
                schema: "orders",
                table: "carts");

            migrationBuilder.DropColumn(
                name: "expires_at",
                schema: "orders",
                table: "carts");

            migrationBuilder.DropColumn(
                name: "metadata",
                schema: "orders",
                table: "carts");

            migrationBuilder.DropColumn(
                name: "session_id",
                schema: "orders",
                table: "carts");

            migrationBuilder.DropColumn(
                name: "added_at",
                schema: "orders",
                table: "cart_items");

            migrationBuilder.DropColumn(
                name: "metadata",
                schema: "orders",
                table: "cart_items");

            migrationBuilder.DropColumn(
                name: "price_snapshot",
                schema: "orders",
                table: "cart_items");

            migrationBuilder.EnsureSchema(
                name: "logs");

            migrationBuilder.RenameColumn(
                name: "product_id",
                schema: "orders",
                table: "cart_items",
                newName: "product_variant_id");

            migrationBuilder.RenameIndex(
                name: "ix_cart_items_product_id",
                schema: "orders",
                table: "cart_items",
                newName: "ix_cart_items_product_variant_id");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                schema: "orders",
                table: "carts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "bts_webhook_events",
                schema: "logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bts_order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    status_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    raw_payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bts_webhook_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    is_sent = table.Column<bool>(type: "boolean", nullable: false),
                    provider_response = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sync_logs",
                schema: "logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    total_processed = table.Column<int>(type: "integer", nullable: false),
                    created_count = table.Column<int>(type: "integer", nullable: false),
                    updated_count = table.Column<int>(type: "integer", nullable: false),
                    skipped_count = table.Column<int>(type: "integer", nullable: false),
                    error_count = table.Column<int>(type: "integer", nullable: false),
                    error_detail = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    response = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    triggered_by = table.Column<long>(type: "bigint", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_sync_logs_users_triggered_by",
                        column: x => x.triggered_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id",
                schema: "logs",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sync_logs_triggered_by",
                schema: "logs",
                table: "sync_logs",
                column: "triggered_by");

            migrationBuilder.AddForeignKey(
                name: "fk_cart_items_product_variants_product_variant_id",
                schema: "orders",
                table: "cart_items",
                column: "product_variant_id",
                principalSchema: "catalogs",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_carts_users_user_id",
                schema: "orders",
                table: "carts",
                column: "user_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cart_items_product_variants_product_variant_id",
                schema: "orders",
                table: "cart_items");

            migrationBuilder.DropForeignKey(
                name: "fk_carts_users_user_id",
                schema: "orders",
                table: "carts");

            migrationBuilder.DropTable(
                name: "bts_webhook_events",
                schema: "logs");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "logs");

            migrationBuilder.DropTable(
                name: "sync_logs",
                schema: "logs");

            migrationBuilder.RenameColumn(
                name: "product_variant_id",
                schema: "orders",
                table: "cart_items",
                newName: "product_id");

            migrationBuilder.RenameIndex(
                name: "ix_cart_items_product_variant_id",
                schema: "orders",
                table: "cart_items",
                newName: "ix_cart_items_product_id");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                schema: "orders",
                table: "carts",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                schema: "orders",
                table: "carts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<JsonDocument>(
                name: "metadata",
                schema: "orders",
                table: "carts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "session_id",
                schema: "orders",
                table: "carts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "added_at",
                schema: "orders",
                table: "cart_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<JsonDocument>(
                name: "metadata",
                schema: "orders",
                table: "cart_items",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_snapshot",
                schema: "orders",
                table: "cart_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "fk_cart_items_products_product_id",
                schema: "orders",
                table: "cart_items",
                column: "product_id",
                principalSchema: "catalogs",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_carts_users_user_id",
                schema: "orders",
                table: "carts",
                column: "user_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
