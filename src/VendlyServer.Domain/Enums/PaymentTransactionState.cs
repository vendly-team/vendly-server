namespace VendlyServer.Domain.Enums;

// Provider tranzaksiyasi holati. Raqamli qiymatlar Payme (Paycom) merchant API
// protokoliga mos (state maydoni) — qayta tartiblanmaydi.
public enum PaymentTransactionState
{
    Created = 1,
    Completed = 2,
    Cancelled = -1,
    CancelledAfterComplete = -2,
}
