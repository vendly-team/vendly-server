using Microsoft.Extensions.Options;

namespace VendlyServer.Infrastructure.Brokers.Smartup;

public class SmartupOptionsValidator : IValidateOptions<SmartupOptions>
{
    public ValidateOptionsResult Validate(string? name, SmartupOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            failures.Add("Smartup:BaseUrl is required");
        if (string.IsNullOrWhiteSpace(options.ImageBaseUrl))
            failures.Add("Smartup:ImageBaseUrl is required");
        if (string.IsNullOrWhiteSpace(options.Token))
            failures.Add("Smartup:Token is required");
        if (string.IsNullOrWhiteSpace(options.PersonId))
            failures.Add("Smartup:PersonId is required");
        if (string.IsNullOrWhiteSpace(options.FilialId))
            failures.Add("Smartup:FilialId is required");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
