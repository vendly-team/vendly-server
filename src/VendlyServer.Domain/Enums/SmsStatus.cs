namespace VendlyServer.Domain.Enums;

public enum SmsStatus
{
    Pending,    // bazaga yozildi, hali Eskiz'ga yuborilmadi
    Waiting,    // Eskiz qabul qildi, operator yetkazishini kutmoqda
    Delivered,  // abonentga yetkazildi (DELIVRD)
    Failed,     // yetkazilmadi (UNDELIV / DELETED)
    Expired,    // muddati o'tdi (EXPIRED)
    Rejected    // rad etildi (REJECTD)
}
