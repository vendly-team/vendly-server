using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Application.Services.Storage;

public class MinioOptionsSetup(IConfiguration configuration) : IConfigureOptions<MinioOptions>
{
    public void Configure(MinioOptions options)
    {
        configuration.GetSection("Minio").Bind(options);
    }
}
