using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Application.Services.Currencies;

public class CurrencyApiOptionsSetup(IConfiguration configuration) : IConfigureOptions<CurrencyApiOptions>
{
    public void Configure(CurrencyApiOptions options)
    {
        configuration.GetSection(CurrencyApiOptions.SectionName).Bind(options);
    }
}
