namespace VendlyServer.Application.Services.Sms.Contracts;

// Eskiz status callback'i (POST data). RequestId = yuborishda qaytgan "id".
public class SmsStatusCallbackRequest
{
    public string? RequestId { get; set; }
    public string? MessageId { get; set; }
    public string? UserSmsId { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Status { get; set; }
    public DateTime? StatusDate { get; set; }
}
