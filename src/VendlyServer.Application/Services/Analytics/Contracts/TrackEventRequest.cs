namespace VendlyServer.Application.Services.Analytics.Contracts;

public sealed record TrackEventRequest(
    string ClientId,
    string EventName,
    Dictionary<string, object> Parameters
);
