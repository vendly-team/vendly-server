namespace VendlyServer.Application.Jobs.Payments;

public interface IClickStatusPollingJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
