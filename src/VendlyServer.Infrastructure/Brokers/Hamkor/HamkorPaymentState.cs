namespace VendlyServer.Infrastructure.Brokers.Hamkor;

/// <summary>Payment states returned by the Hamkorbank acquiring API (the "state" field).</summary>
public enum HamkorPaymentState
{
    Created = 1,
    Holded = 2,
    Confirmed = 3,
    Canceled = 4,
    Returned = 5,
    InProgress = 13,
    Payed = 14
}
