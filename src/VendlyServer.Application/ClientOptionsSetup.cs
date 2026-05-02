using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Application;

public class ClientOptionsSetup(IConfiguration configuration) : IConfigureOptions<ClientOptions>
{
    public void Configure(ClientOptions options)
    {
        configuration.GetSection(ClientOptions.SectionName).Bind(options);
    }
}
