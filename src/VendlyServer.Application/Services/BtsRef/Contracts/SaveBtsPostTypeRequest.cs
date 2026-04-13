namespace VendlyServer.Application.Services.BtsRef.Contracts;

public class SaveBtsPostTypeRequest
{
    public int BtsId { get; set; }
    public string Name { get; set; } = string.Empty;
}
