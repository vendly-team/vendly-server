namespace VendlyServer.Application.Services.SyncLogs.Contracts;

public record SyncLogDetailResponse(
    long Id,
    string Source,
    int Status,
    int TotalProcessed,
    int CreatedCount,
    int UpdatedCount,
    int ErrorCount,
    int? DurationMs,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string? RequestUrl,
    string? RequestBody,
    string? Response,
    string? ErrorDetail
);
