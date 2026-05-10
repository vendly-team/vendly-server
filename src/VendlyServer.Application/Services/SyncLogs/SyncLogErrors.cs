using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.SyncLogs;

public static class SyncLogErrors
{
    public static readonly Error NotFound = Error.NotFound("SyncLog.NotFound");
}
