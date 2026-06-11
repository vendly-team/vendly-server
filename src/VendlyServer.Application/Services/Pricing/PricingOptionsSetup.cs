using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Application.Services.Pricing;

public class PricingOptionsSetup(IConfiguration configuration) : IConfigureOptions<PricingOptions>
{
    public void Configure(PricingOptions options)
    {
        configuration.GetSection(PricingOptions.SectionName).Bind(options);
    }
}
