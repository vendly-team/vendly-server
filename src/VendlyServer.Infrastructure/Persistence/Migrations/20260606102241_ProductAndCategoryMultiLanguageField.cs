using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendlyServer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductAndCategoryMultiLanguageField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE catalogs.products
                ALTER COLUMN name TYPE jsonb
                USING json_build_object('uz', name, 'ru', name, 'en', name, 'cyrl', name)::jsonb;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE catalogs.categories
                ALTER COLUMN name TYPE jsonb
                USING json_build_object('uz', name, 'ru', name, 'en', name, 'cyrl', name)::jsonb;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE catalogs.products
                ALTER COLUMN name TYPE character varying(255)
                USING (name->>'uz')::character varying;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE catalogs.categories
                ALTER COLUMN name TYPE character varying(255)
                USING (name->>'uz')::character varying;
                """);
        }
    }
}
