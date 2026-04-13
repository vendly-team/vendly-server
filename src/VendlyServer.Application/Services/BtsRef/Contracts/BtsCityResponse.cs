namespace VendlyServer.Application.Services.BtsRef.Contracts;

public class BtsCityResponse
{
    public long Id { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime SyncedAt { get; set; }
}
