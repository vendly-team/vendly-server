using System.ComponentModel.DataAnnotations;

namespace VendlyServer.Infrastructure.Brokers.Smartup;

public class SmartupOptions
{
    public const string SectionName = "Smartup";

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, Url]
    public string ImageBaseUrl { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string PersonId { get; set; } = string.Empty;

    [Required]
    public string FilialId { get; set; } = string.Empty;
}
