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
    public string DefaultPickupType { get; set; } = "self";
    public string DefaultDropoffType { get; set; } = "courier";
    public int DefaultPackageId { get; set; } = 7;
    public int DefaultPostTypeId { get; set; } = 7;
}
