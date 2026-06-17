using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Infrastructure.Payments;

public class PaymentsOptionsSetup(IConfiguration configuration) : IConfigureOptions<PaymentsOptions>
{
    public void Configure(PaymentsOptions options)
    {
        configuration.GetSection(PaymentsOptions.SectionName).Bind(options);
    }
}
