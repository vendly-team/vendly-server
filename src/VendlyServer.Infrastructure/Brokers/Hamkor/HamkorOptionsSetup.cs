using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Infrastructure.Brokers.Hamkor;

public class HamkorOptionsSetup(IConfiguration configuration) : IConfigureOptions<HamkorOptions>
{
    public void Configure(HamkorOptions options)
    {
        configuration.GetSection(HamkorOptions.SectionName).Bind(options);
    }
}
