using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Application.Services.Storage;

public class StorageOptionsSetup(IConfiguration configuration) : IConfigureOptions<StorageOptions>
{
    public void Configure(StorageOptions options)
    {
        configuration.GetSection("Storage").Bind(options);
    }
}
