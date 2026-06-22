namespace VendlyServer.Application.Services.Sms.Contracts;

public class SendSmsRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // Ixtiyoriy — SMS qaysi foydalanuvchiga bog'liq (OTP'da null bo'lishi mumkin).
    public long? UserId { get; set; }
}
