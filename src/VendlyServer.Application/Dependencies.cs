using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddScoped<IStorageService, LocalStorageService>();
        return services;
    }
}
