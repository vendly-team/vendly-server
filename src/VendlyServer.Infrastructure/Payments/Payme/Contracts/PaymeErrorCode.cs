namespace VendlyServer.Infrastructure.Payments.Payme.Contracts;

// Xato kodlari — Payme (Paycom) merchant API spetsifikatsiyasi.
public enum PaymeErrorCode
{
    InvalidAmount = -31001,
    TransactionNotFound = -31003,
    CouldNotCancel = -31007,
    CouldNotPerform = -31008,
    InvalidAccount = -31050,
    InsufficientPrivilege = -32504,
    InvalidJsonRpc = -32600,
    MethodNotFound = -32601,
}
