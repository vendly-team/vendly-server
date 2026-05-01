namespace VendlyServer.Application.Services.Storage;

public class MinioOptions
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "vendly";
    public string PublicBaseUrl { get; set; } = "http://localhost:9000";
    public bool UseSsl { get; set; } = false;
}
