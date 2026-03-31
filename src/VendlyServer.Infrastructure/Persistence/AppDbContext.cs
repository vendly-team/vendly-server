namespace VendlyServer.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Register DbSet<Entity> properties here:
    // public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite key examples:
        // modelBuilder.Entity<SomeEntity>()
        //     .HasKey(x => new { x.EntityId, x.UserId });
    }

    private void TrackActionsAt()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.GetType()
                    .GetProperty("CreatedAt")?.SetValue(entry.Entity, DateTime.UtcNow);

            if (entry.State == EntityState.Modified)
                entry.Entity.GetType()
                    .GetProperty("UpdatedAt")?.SetValue(entry.Entity, DateTime.UtcNow);
        }
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        TrackActionsAt();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
