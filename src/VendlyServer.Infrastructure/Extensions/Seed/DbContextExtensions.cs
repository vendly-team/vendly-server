using Microsoft.EntityFrameworkCore;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Authentication;
using VendlyServer.Infrastructure.Persistence;
using VendlyServer.Domain.Entities.Orders;

namespace VendlyServer.Infrastructure.Extensions.Seed;

public static class DbContextExtensions
{
    extension(AppDbContext dbContext)
    {
        public async Task SeedAsync(bool seedSampleCatalog)
        {
            IPasswordHasher passwordHasher = new PasswordHasher();

            await dbContext.SeedUsersAsync(passwordHasher);
            await dbContext.SeedBtsRefAsync();
            await dbContext.SeedReturnReasonsAsync();

            if (seedSampleCatalog)
            {
                await dbContext.SeedCatalogAsync();
                await dbContext.SeedTestPaymentProductAsync();
            }
        }

        private async Task SeedCatalogAsync()
        {
            var hasCategories = await dbContext.Categories.AnyAsync();
            if (hasCategories) return;

            var category = new Category
            {
                Name = "Telefonlar",
                Slug = "telefonlar",
                IsActive = true,
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            var product = new Product
            {
                CategoryId = category.Id,
                Name = "Samsung Galaxy S26 Ultra 5G",
                Description = "Test mahsulot — HamkorPayment integratsiyasini sinash uchun.",
                SyncSource = SyncSource.Manual,
                IsActive = true,
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            var variant = new ProductVariant
            {
                ProductId = product.Id,
                Name = "12/512 GB",
                Price = 16_499_000m,
                Quantity = 50,
                IsActive = true,
            };

            dbContext.ProductVariants.Add(variant);
            await dbContext.SaveChangesAsync();
        }

        // Test to'lov uchun arzon mahsulot (Click/Hamkor sandbox sinovlari uchun).
        // O'lchamlari to'liq — BTS yetkazib berish narxi hisoblana oladi.
        private async Task SeedTestPaymentProductAsync()
        {
            const string testProductName = "Test Payment Product (1000 so'm)";
            // Name — MultiLanguageField (owned). EF translate qila olishi uchun Uz fieldi orqali tekshiramiz.
            // Implicit operator (string → MultiLanguageField) Uz/Ru/En/Cyrl ga bir xil string yozadi.
            var exists = await dbContext.Products.AnyAsync(p => p.Name.Uz == testProductName);
            if (exists) return;

            // Category tanlash tartibi:
            //   1) "telefonlar" slug (fresh DB'da bo'ladi)
            //   2) Mavjud bo'lgan eng birinchi active category (Smartup sync'idan kelgani)
            //   3) Hech qaysi bo'lmasa — alohida "Test Products" category yaratamiz
            var category =
                await dbContext.Categories.FirstOrDefaultAsync(c => c.Slug == "telefonlar" && !c.IsDeleted)
                ?? await dbContext.Categories.Where(c => c.IsActive && !c.IsDeleted).FirstOrDefaultAsync();

            if (category is null)
            {
                category = new Category
                {
                    Name = "Test Products",
                    Slug = "test-products",
                    IsActive = true,
                };
                dbContext.Categories.Add(category);
                await dbContext.SaveChangesAsync();
            }

            var product = new Product
            {
                CategoryId = category.Id,
                Name = testProductName,
                Description = "Click/Hamkor to'lovni sinash uchun arzon test mahsuloti.",
                SyncSource = SyncSource.Manual,
                IsActive = true,
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            var variant = new ProductVariant
            {
                ProductId = product.Id,
                Name = "Default",
                Price = 1_000m,
                Quantity = 9999,
                IsActive = true,
            };

            dbContext.ProductVariants.Add(variant);
            await dbContext.SaveChangesAsync();

            var measurement = new ProductMeasurement
            {
                ProductVariantId = variant.Id,
                WeightKg = 0.5m,
                LengthCm = 20m,
                WidthCm = 15m,
                HeightCm = 5m,
            };

            dbContext.ProductMeasurements.Add(measurement);
            await dbContext.SaveChangesAsync();
        }

        private async Task SeedReturnReasonsAsync()
        {
            var existingKeys = await dbContext.ReturnReasons
                .Where(r => !r.IsDeleted)
                .Select(r => r.Key)
                .ToListAsync();

            var toInsert = ReturnReasonSeedData.All()
                .Where(r => !existingKeys.Contains(r.Key))
                .ToList();

            if (toInsert.Count == 0) return;

            dbContext.ReturnReasons.AddRange(toInsert);
            await dbContext.SaveChangesAsync();
        }

        private async Task SeedBtsRefAsync()
        {
            var hasRegions = await dbContext.BtsRegions.AnyAsync();
            if (hasRegions) return;

            var now = DateTime.UtcNow;

            dbContext.BtsRegions.AddRange(BtsRefSeedData.Regions(now));
            await dbContext.SaveChangesAsync();

            dbContext.BtsCities.AddRange(BtsRefSeedData.Cities(now));
            await dbContext.SaveChangesAsync();
        }

        private async Task SeedUsersAsync(IPasswordHasher passwordHasher)
        {
            var seedUsers = new[]
            {
                new { FirstName = "Customer",  LastName = "User", Email="user@test.com",   Phone = "+998900000001", Password = "user123",    Role = UserRole.Customer },
                new { FirstName = "Admin",     LastName = "User", Email="admin@test.com",  Phone = "+998900000002", Password = "admin123",   Role = UserRole.Admin    },
                new { FirstName = "Manager",   LastName = "User", Email="manager@test.com",Phone = "+998900000003", Password = "manager123", Role = UserRole.Manager  },
            };

            var phones = seedUsers.Select(u => u.Phone).ToList();

            var existingPhones = await dbContext.Users
                .Where(u => phones.Contains(u.Phone))
                .Select(u => u.Phone)
                .ToListAsync();

            var toInsert = seedUsers
                .Where(u => !existingPhones.Contains(u.Phone))
                .Select(u => new User
                {
                    FirstName    = u.FirstName,
                    LastName     = u.LastName,
                    Phone        = u.Phone,
                    Email        = u.Email,
                    PasswordHash = passwordHasher.Hash(u.Password),
                    Role         = u.Role,
                })
                .ToList();

            if (toInsert.Count == 0) return;

            dbContext.Users.AddRange(toInsert);
            await dbContext.SaveChangesAsync();
        }
    }
}
