using System.Text.Json;
using VendlyServer.Domain.Entities.Catalog;
using VendlyServer.Domain.Entities.Logs;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Entities.Ref;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // public
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<CustomerAddress> CustomerAddresses { get; set; }

    // catalog
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductMeasurement> ProductMeasurements { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductSpec> ProductSpecs { get; set; }
    public DbSet<ProductSyncMeta> ProductSyncMetas { get; set; }
    public DbSet<ProductFieldOverride> ProductFieldOverrides { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<DiscountProduct> DiscountProducts { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<Review> Reviews { get; set; }

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

    // logs
    public DbSet<SyncLog> SyncLogs { get; set; }
    public DbSet<BtsWebhookEvent> BtsWebhookEvents { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }

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

        modelBuilder.Entity<Category>()
            .HasIndex(x => x.Slug).IsUnique();

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.SalePrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProductMeasurement>(entity =>
        {
            entity.HasIndex(x => x.ProductId).IsUnique();
            entity.Property(x => x.WeightKg).HasPrecision(10, 3);
            entity.Property(x => x.LengthCm).HasPrecision(10, 2);
            entity.Property(x => x.WidthCm).HasPrecision(10, 2);
            entity.Property(x => x.HeightCm).HasPrecision(10, 2);
            entity.Property(x => x.VolumeCm3).HasPrecision(14, 2);
        });

        modelBuilder.Entity<ProductSyncMeta>(entity =>
        {
            entity.HasIndex(x => x.ProductId).IsUnique();
            entity.Property(x => x.ExtPrice).HasPrecision(18, 2);
            entity.Property(x => x.ExtWeightKg).HasPrecision(10, 3);
            entity.Property(x => x.ExtLengthCm).HasPrecision(10, 2);
            entity.Property(x => x.ExtWidthCm).HasPrecision(10, 2);
            entity.Property(x => x.ExtHeightCm).HasPrecision(10, 2);
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

        // === Enum to string conversions ===
        modelBuilder.Entity<User>()
            .Property(x => x.Role).HasConversion<string>().HasMaxLength(50);

        modelBuilder.Entity<Product>()
            .Property(x => x.SyncSource).HasConversion<string>().HasMaxLength(50);

        modelBuilder.Entity<ProductSyncMeta>()
            .Property(x => x.LastSyncStatus).HasConversion<string>().HasMaxLength(50);

        modelBuilder.Entity<Discount>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Scope).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<Review>()
            .Property(x => x.Status).HasConversion<string>().HasMaxLength(50);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.DeliveryStatus).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<OrderStatusHistory>()
            .Property(x => x.Status).HasConversion<string>().HasMaxLength(50);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(x => x.Provider).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<OrderCancellation>()
            .Property(x => x.ReasonCode).HasConversion<string>().HasMaxLength(50);

        modelBuilder.Entity<OrderReturn>(entity =>
        {
            entity.Property(x => x.ReasonCode).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<SyncLog>()
            .Property(x => x.Status).HasConversion<string>().HasMaxLength(50);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(50);
        });

        // === JSONB column types ===
        modelBuilder.Entity<CustomerAddress>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<Category>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<Product>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<ProductMeasurement>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<ProductImage>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<ProductSpec>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<ProductSyncMeta>()
            .Property(x => x.RawPayload).HasColumnType("jsonb");

        modelBuilder.Entity<Discount>()
            .Property(x => x.Metadata).HasColumnType("jsonb");

        modelBuilder.Entity<Review>()
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

        modelBuilder.Entity<SyncLog>()
            .Property(x => x.ErrorDetail).HasColumnType("jsonb");

        modelBuilder.Entity<BtsWebhookEvent>()
            .Property(x => x.RawPayload).HasColumnType("jsonb");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(x => x.OldValue).HasColumnType("jsonb");
            entity.Property(x => x.NewValue).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Notification>()
            .Property(x => x.ProviderResponse).HasColumnType("jsonb");
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
