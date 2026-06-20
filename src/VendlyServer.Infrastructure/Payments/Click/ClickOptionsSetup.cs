using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Infrastructure.Payments.Click;

public class ClickOptionsSetup(IConfiguration configuration) : IConfigureOptions<ClickOptions>
{
    public void Configure(ClickOptions options)
    {
        configuration.GetSection(ClickOptions.SectionName).Bind(options);
    }
}
