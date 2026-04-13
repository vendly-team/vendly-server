namespace VendlyServer.Application.Services.BtsRef.Contracts;

public class SaveBtsCityRequest
{
    public string RegionCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
