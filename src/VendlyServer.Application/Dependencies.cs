using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using VendlyServer.Application.Jobs.Auth;
using VendlyServer.Application.Jobs.BtsCatalog;
using VendlyServer.Application.Jobs.SmartupCatalog;
using VendlyServer.Application.Services.SmartupSync;
using VendlyServer.Application.Services.Auth;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.Products;
using VendlyServer.Application.Services.Telegram;
using VendlyServer.Application.Services.Users;
using VendlyServer.Application.Services.RecentlyViewed;
using VendlyServer.Application.Services.Storages;
using VendlyServer.Application.Services.Wishlists;
using VendlyServer.Application.Services.Currencies;
using VendlyServer.Application.Services.Addresses;
using VendlyServer.Application.Services.Categories;
using VendlyServer.Application.Services.Carts;
using VendlyServer.Application.Services.Checkout;
using VendlyServer.Application.Services.Analytics;
using VendlyServer.Application.Services.SyncLogs;

namespace VendlyServer.Application;

public static class Dependencies
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services)
    {
        services.ConfigureOptions<ClientOptionsSetup>();
        services.ConfigureStorage();
        services.ConfigureCurrency();
        services.ConfigureTelegram();

        services.AddValidatorsFromAssembly(typeof(Dependencies).Assembly);

        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBtsRefService, BtsRefService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IRecentlyViewedService, RecentlyViewedService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<ICurrencyConverterService, CurrencyConverterService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<ISyncLogService, SyncLogService>();

        services.AddScoped<IBtsCatalogSyncJob, BtsCatalogSyncJob>();
        services.AddScoped<ICleanExpiredRefreshTokensJob, CleanExpiredRefreshTokensJob>();
        services.AddScoped<ISmartupSyncService, SmartupSyncService>();
        services.AddScoped<ISmartupCatalogSyncJob, SmartupCatalogSyncJob>();

        return services;
    }

    private static IServiceCollection ConfigureStorage(this IServiceCollection services)
    {
        services.ConfigureOptions<StorageOptionsSetup>();
        services.ConfigureOptions<MinioOptionsSetup>();

        services.AddSingleton<IMinioClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MinioOptions>>().Value;

            var builder = new MinioClient()
                .WithEndpoint(opts.Endpoint)
                .WithCredentials(opts.AccessKey, opts.SecretKey);

            if (opts.UseSsl)
                builder = builder.WithSSL();

            return builder.Build();
        });

        services.AddScoped<IStorageService, MinioStorageService>();
        return services;
    }

    private static IServiceCollection ConfigureCurrency(this IServiceCollection services)
    {
        services.ConfigureOptions<CurrencyApiOptionsSetup>();
        services.AddMemoryCache();
        services.AddHttpClient<ICurrencyApiClient, CurrencyApiClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<CurrencyApiOptions>>().Value;

            client.BaseAddress = new Uri(opts.BaseUrl);
            client.DefaultRequestHeaders.Add("apikey", opts.ApiKey);
        });

        return services;
    }

    private static IServiceCollection ConfigureTelegram(this IServiceCollection services)
    {
        services.ConfigureOptions<TelegramBotOptionsSetup>();

        services.AddHttpClient<ITelegramImageUrlValidator, TelegramImageUrlValidator>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(2);
        });

        services.AddHttpClient<ITelegramBotClient, TelegramBotClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
            client.BaseAddress = new Uri($"https://api.telegram.org/bot{opts.Token}/");
        });

        services.AddScoped<ITelegramUpdateHandler, TelegramUpdateHandler>();
        services.AddHostedService<TelegramWebhookHostedService>();

        return services;
    }
}
