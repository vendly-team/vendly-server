namespace VendlyServer.Infrastructure.Payments.Click.Contracts;

// Xato kodlari — Click SHOP API spec ("error" maydonida qaytariladi).
public enum ClickErrorCode
{
    Success = 0,
    SignCheckFailed = -1,
    IncorrectParameterAmount = -2,
    ActionNotFound = -3,
    AlreadyPaid = -4,
    UserDoesNotExist = -5,
    TransactionDoesNotExist = -6,
    ErrorInRequestFromClick = -8,
    TransactionCancelled = -9,
}
