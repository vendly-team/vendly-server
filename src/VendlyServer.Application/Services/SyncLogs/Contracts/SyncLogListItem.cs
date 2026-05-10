namespace VendlyServer.Application.Services.SyncLogs.Contracts;

public record SyncLogListItem(
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
    string? RequestUrl
);
