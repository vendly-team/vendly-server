namespace VendlyServer.Infrastructure.Brokers.BtsExpress;

public class BtsExpressOptions
{
    public const string SectionName = "BtsExpress";

    public string BaseUrl { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderPhone { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public string SenderCityCode { get; set; } = string.Empty;
    public string SenderBranchCode { get; set; } = string.Empty;
    public string DefaultPickupType { get; set; } = string.Empty;
    public int DefaultPackageId { get; set; } = 7;
    public int DefaultPostTypeId { get; set; } = 7;

    // Optional shared secret to validate the BTS status webhook (empty = accept all).
    public string WebhookSecretToken { get; set; } = string.Empty;
}
