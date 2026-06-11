namespace VendlyServer.Infrastructure.Brokers.Cbu.Contracts.Responses;

public record CbuUsdRateResponse(
    decimal Rate,
    decimal Diff,
    string Date);
