using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendlyServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailUniqueIndexMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "public",
                table: "users",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                schema: "public",
                table: "users");
        }
    }
}
