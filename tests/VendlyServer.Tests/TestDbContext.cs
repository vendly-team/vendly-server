using Microsoft.EntityFrameworkCore;
using VendlyServer.Domain.Entities.Catalog;

namespace VendlyServer.Tests;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Wishlist>()
            .HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
    }
}
