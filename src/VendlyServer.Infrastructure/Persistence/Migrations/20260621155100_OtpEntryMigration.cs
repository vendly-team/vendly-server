using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VendlyServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OtpEntryMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "otps",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    resend_count = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    sms_message_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_otps", x => x.id);
                    table.ForeignKey(
                        name: "fk_otps_sms_messages_sms_message_id",
                        column: x => x.sms_message_id,
                        principalSchema: "logs",
                        principalTable: "sms_messages",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_otps_sms_message_id",
                schema: "public",
                table: "otps",
                column: "sms_message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "otps",
                schema: "public");
        }
    }
}
