namespace VendlyServer.Application.Services.BtsRef.Contracts;

public class BtsPostTypeResponse
{
    public long Id { get; set; }
    public int BtsId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime SyncedAt { get; set; }
}
