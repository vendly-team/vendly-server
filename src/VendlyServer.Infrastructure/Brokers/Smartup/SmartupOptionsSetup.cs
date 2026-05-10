using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Infrastructure.Brokers.Smartup;

public class SmartupOptionsSetup(IConfiguration configuration) : IConfigureOptions<SmartupOptions>
{
    public void Configure(SmartupOptions options)
    {
        configuration.GetSection(SmartupOptions.SectionName).Bind(options);
    }
}
