using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Infrastructure.Brokers.BtsExpress;

public class BtsExpressOptionsSetup(IConfiguration configuration) : IConfigureOptions<BtsExpressOptions>
{
    public void Configure(BtsExpressOptions options)
    {
        configuration.GetSection(BtsExpressOptions.SectionName).Bind(options);
    }
}
