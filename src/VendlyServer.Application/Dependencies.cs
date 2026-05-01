using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using VendlyServer.Application.Jobs.BtsCatalog;
using VendlyServer.Application.Services.Auth;
using VendlyServer.Application.Services.BtsRef;
using VendlyServer.Application.Services.Category;
using VendlyServer.Application.Services.Products;
using VendlyServer.Application.Services.Storage;
using VendlyServer.Application.Services.Users;
using VendlyServer.Application.Services.Wishlist;

namespace VendlyServer.Application;

public static class Dependencies
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services)
    {
        services.ConfigureStorage();

        services.AddValidatorsFromAssembly(typeof(Dependencies).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBtsRefService, BtsRefService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProductService, ProductService>();

        services.AddScoped<IBtsCatalogSyncJob, BtsCatalogSyncJob>();

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
}
