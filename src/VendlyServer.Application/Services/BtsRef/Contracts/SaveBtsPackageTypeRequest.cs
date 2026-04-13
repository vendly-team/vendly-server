namespace VendlyServer.Application.Services.BtsRef.Contracts;

public class SaveBtsPackageTypeRequest
{
    public int BtsId { get; set; }
    public string Name { get; set; } = string.Empty;
}
