using System.Text.Json;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Entities.Ref;

namespace VendlyServer.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // public
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    #region Catalog

    public DbSet<Category> Categories { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<DiscountProduct> DiscountProducts { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductMeasurement> ProductMeasurements { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<VariantOption> VariantOptions { get; set; }
    public DbSet<VariantOptionValue> VariantOptionValues { get; set; }
    public DbSet<VariantType> VariantTypes { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    
    #endregion Catalog

    // orders
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<OrderNote> OrderNotes { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<OrderCancellation> OrderCancellations { get; set; }
    public DbSet<OrderReturn> OrderReturns { get; set; }
    public DbSet<OrderReturnItem> OrderReturnItems { get; set; }

    // ref
    public DbSet<BtsRegionRef> BtsRegions { get; set; }
    public DbSet<BtsCityRef> BtsCities { get; set; }
    public DbSet<BtsBranchRef> BtsBranches { get; set; }
    public DbSet<BtsPackageTypeRef> BtsPackageTypes { get; set; }
    public DbSet<BtsPostTypeRef> BtsPostTypes { get; set; }
    public DbSet<Address> Addresses { get; set; }

    // logs
    //public DbSet<SyncLog> SyncLogs { get; set; }
    //public DbSet<BtsWebhookEvent> BtsWebhookEvents { get; set; }
    //public DbSet<NotificationLog> NotificationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // === Composite Keys ===
        modelBuilder.Entity<DiscountProduct>()
            .HasKey(x => new { x.DiscountId, x.ProductId });

        // === Unique Indexes ===
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Phone).IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.Token).IsUnique();

        modelBuilder.Entity<ProductMeasurement>(entity =>
        {
            entity.HasIndex(x => x.ProductVariantId).IsUnique();
            entity.Property(x => x.WeightKg).HasPrecision(10, 3);
            entity.Property(x => x.LengthCm).HasPrecision(10, 2);
            entity.Property(x => x.WidthCm).HasPrecision(10, 2);
            entity.Property(x => x.HeightCm).HasPrecision(10, 2);
            entity.Property(x => x.VolumeCm3).HasPrecision(14, 2);
        });
        
        modelBuilder.Entity<Wishlist>()
            .HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();

        modelBuilder.Entity<Discount>()
            .Property(x => x.Value).HasPrecision(10, 2);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.DeliveryCost).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(x => x.WeightKgSnap).HasPrecision(10, 3);
            entity.Property(x => x.PriceSnap).HasPrecision(18, 2);
            entity.Property(x => x.TotalSnap).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CartItem>()
            .Property(x => x.PriceSnapshot).HasPrecision(18, 2);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderCancellation>()
            .HasIndex(x => x.OrderId).IsUnique();

        modelBuilder.Entity<OrderReturn>()
            .HasIndex(x => x.OrderId).IsUnique();

        // ref unique indexes
        modelBuilder.Entity<BtsRegionRef>()
            .HasIndex(x => x.Code).IsUnique();

        modelBuilder.Entity<BtsCityRef>()
            .HasIndex(x => x.Code).IsUnique();

        modelBuilder.Entity<BtsBranchRef>()
            .HasIndex(x => x.Code).IsUnique();

        modelBuilder.Entity<BtsPackageTypeRef>()
            .HasIndex(x => x.BtsId).IsUnique();

        modelBuilder.Entity<BtsPostTypeRef>()
            .HasIndex(x => x.BtsId).IsUnique();

        // === JSONB column types ===
        
        modelBuilder.Entity<Category>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<Product>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<ProductMeasurement>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<ProductVariant>()
            .Property(x => x.Images).HasColumnType("jsonb");

        modelBuilder.Entity<ProductVariant>()
            .Property(x => x.Metadata).HasColumnType("jsonb");


        modelBuilder.Entity<Cart>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<CartItem>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<Order>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<OrderItem>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<Payment>()
            .Property(x => x.ProviderResponse).HasColumnType("jsonb");

        modelBuilder.Entity<OrderCancellation>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<OrderReturn>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<BtsBranchRef>()
            .Property(x => x.WorkingHours).HasColumnType("jsonb");

        //modelBuilder.Entity<SyncLog>()
        //    .Property(x => x.ErrorDetail).HasColumnType("jsonb");

        //modelBuilder.Entity<BtsWebhookEvent>()
        //    .Property(x => x.RawPayload).HasColumnType("jsonb");

        //modelBuilder.Entity<NotificationLog>()
        //    .Property(x => x.ProviderResponse).HasColumnType("jsonb");

        // Value converters for JsonDocument when using non-PostgreSQL providers (e.g. InMemory in tests)
        if (Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var jsonConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<JsonDocument, string>(
                v => v.RootElement.GetRawText(),
                v => JsonDocument.Parse(v));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                foreach (var property in entityType.GetProperties()
                             .Where(p => p.ClrType == typeof(JsonDocument)))
                    property.SetValueConverter(jsonConverter);
        }
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
