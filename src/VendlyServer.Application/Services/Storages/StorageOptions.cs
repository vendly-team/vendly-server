namespace VendlyServer.Application.Services.Storages;

public class StorageOptions
{
    public string BasePath { get; set; } = "wwwroot/uploads";
    public string BaseUrl { get; set; } = "/uploads";
    public long MaxFileSizeBytes { get; set; } = 10_485_760;
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp", ".svg", ".pdf"];
}
