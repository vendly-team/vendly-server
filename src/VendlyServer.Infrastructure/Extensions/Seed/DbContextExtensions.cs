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
            await dbContext.SeedHeroBannersAsync();

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

            // DIQQAT: ProductPricingService variant.Price'ni USD deb qabul qiladi va
            // CBU kursi orqali so'mga aylantiradi (PricingContext.CalculateSoumPrice).
            // Test uchun so'mda ~1000-1500 chiqishi uchun 0.08 USD yozamiz (≈1000 so'm bugungi kursda).
            // Kurs o'zgarsa narx bir oz farq qiladi, lekin baribir Click test uchun yetarli darajada arzon.
            var variant = new ProductVariant
            {
                ProductId = product.Id,
                Name = "Default",
                Price = 0.08m, // USD — ≈1000 so'm
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

        private async Task SeedHeroBannersAsync()
        {
            var hasBanners = await dbContext.HeroBanners.AnyAsync();
            if (hasBanners) return;

            var banners = new[]
            {
                new HeroBanner
                {
                    Title = new MultiLanguageField
                    {
                        Uz = "Muzlatgichlar uchun ajoyib chegirmalar",
                        Ru = "Отличные скидки на холодильники",
                        En = "Amazing deals on refrigerators",
                        Cyrl = "Музлатгичлар учун ажойиб чегирмалар"
                    },
                    Subtitle = new MultiLanguageField
                    {
                        Uz = "Zamonaviy muzlatgichlarni optimum narxlarda oling",
                        Ru = "Купите современные холодильники по оптимальным ценам",
                        En = "Get modern refrigerators at optimal prices",
                        Cyrl = "Замонавий музлатгичларни оптимум нархларда олинг"
                    },
                    BadgeText = new MultiLanguageField
                    {
                        Uz = "-20%",
                        Ru = "-20%",
                        En = "-20%",
                        Cyrl = "-20%"
                    },
                    CtaText = new MultiLanguageField
                    {
                        Uz = "Xarid qilish",
                        Ru = "Купить",
                        En = "Shop now",
                        Cyrl = "Харид қилиш"
                    },
                    CtaLink = "/category/refrigerators",
                    ImageUrl = "/banners/hero1.png", // served from Api/wwwroot/banners (old landing-page banner)
                    SortOrder = 0,
                    IsActive = true,
                },
                new HeroBanner
                {
                    Title = new MultiLanguageField
                    {
                        Uz = "Kir mashinalarida super aksiya",
                        Ru = "Супер акция на стиральные машины",
                        En = "Super sale on washing machines",
                        Cyrl = "Кир машиналарида супер акция"
                    },
                    Subtitle = new MultiLanguageField
                    {
                        Uz = "Energy-sarflagi past kir mashinalarini tanlang",
                        Ru = "Выбирайте энергоэффективные стиральные машины",
                        En = "Choose energy-efficient washing machines",
                        Cyrl = "Энергия-сарфлаги паст кир машиналарини танланг"
                    },
                    BadgeText = null,
                    CtaText = new MultiLanguageField
                    {
                        Uz = "Ko'rish",
                        Ru = "Смотреть",
                        En = "View deals",
                        Cyrl = "Кўриш"
                    },
                    CtaLink = "/category/washing-machines",
                    ImageUrl = "/banners/hero2.png", // served from Api/wwwroot/banners (old landing-page banner)
                    SortOrder = 1,
                    IsActive = true,
                },
                new HeroBanner
                {
                    Title = new MultiLanguageField
                    {
                        Uz = "Televizorlar uchun maxsus taklif",
                        Ru = "Специальное предложение на телевизоры",
                        En = "Special offer on televisions",
                        Cyrl = "Телевизорлар учун махсус таклиф"
                    },
                    Subtitle = new MultiLanguageField
                    {
                        Uz = "4K va OLED televizorlarni eng yaxshi narxlarda",
                        Ru = "4K и OLED телевизоры по лучшим ценам",
                        En = "4K and OLED TVs at the best prices",
                        Cyrl = "4K ва OLED телевизорларни энг яхши нархларда"
                    },
                    BadgeText = null,
                    CtaText = new MultiLanguageField
                    {
                        Uz = "Ko'rish",
                        Ru = "Смотреть",
                        En = "View deals",
                        Cyrl = "Кўриш"
                    },
                    CtaLink = "/category/televisions",
                    ImageUrl = "/banners/hero3.png", // served from Api/wwwroot/banners (old landing-page banner)
                    SortOrder = 2,
                    IsActive = true,
                },
            };

            dbContext.HeroBanners.AddRange(banners);
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
