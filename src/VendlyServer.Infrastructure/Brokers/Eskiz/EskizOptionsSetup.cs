using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Infrastructure.Brokers.Eskiz;

public class EskizOptionsSetup(IConfiguration configuration) : IConfigureOptions<EskizOptions>
{
    public void Configure(EskizOptions options)
    {
        configuration.GetSection(EskizOptions.SectionName).Bind(options);
    }
}
