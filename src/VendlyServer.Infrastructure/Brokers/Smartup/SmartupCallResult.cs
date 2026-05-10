using System.Text.Json;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Infrastructure.Brokers.Smartup;

public record SmartupCallResult<T>(
    Result<T> Result,
    string Url,
    JsonDocument RequestBody,
    JsonDocument? ResponseBody,
    int DurationMs,
    bool HttpSuccess,
    DateTime StartedAt,
    DateTime FinishedAt
);
