namespace VendlyServer.Domain.Enums;

// Bekor qilish sabablari — Payme (Paycom) merchant API spetsifikatsiyasi (reason maydoni).
public enum PaymentTransactionCancelReason
{
    ReceiverNotFound = 1,
    DebitFailed = 2,
    TransactionError = 3,
    TimedOut = 4,
    Refund = 5,
    UnknownError = 10,
}
