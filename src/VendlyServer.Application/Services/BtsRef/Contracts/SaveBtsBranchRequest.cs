namespace VendlyServer.Application.Services.BtsRef.Contracts;

public class SaveBtsBranchRequest
{
    public string RegionCode { get; set; } = string.Empty;
    public string CityCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LatLong { get; set; }
}
