using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace VendlyServer.Infrastructure.Analytics;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    public string MeasurementId { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public bool Enabled { get; init; }
}

public sealed class AnalyticsOptionsSetup(IConfiguration configuration) : IConfigureOptions<AnalyticsOptions>
{
    public void Configure(AnalyticsOptions options)
        => configuration.GetSection(AnalyticsOptions.SectionName).Bind(options);
}
