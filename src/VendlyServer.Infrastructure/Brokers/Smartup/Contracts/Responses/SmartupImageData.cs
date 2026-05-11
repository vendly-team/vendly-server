namespace VendlyServer.Infrastructure.Brokers.Smartup.Contracts.Responses;

public record SmartupImageData(Stream Content, string ContentType, long Size);
