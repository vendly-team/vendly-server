using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Infrastructure.Payments.Payme;

public class PaymeOptionsSetup(IConfiguration configuration) : IConfigureOptions<PaymeOptions>
{
    public void Configure(PaymeOptions options)
    {
        configuration.GetSection(PaymeOptions.SectionName).Bind(options);
    }
}
