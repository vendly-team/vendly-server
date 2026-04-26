namespace VendlyServer.Application.Services.Storage;

public class StorageOptions
{
    public string BasePath { get; set; } = "wwwroot/uploads";
    public string BaseUrl { get; set; } = "/uploads";
    public long MaxFileSizeBytes { get; set; } = 5_242_880;
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp", ".svg"];
}
