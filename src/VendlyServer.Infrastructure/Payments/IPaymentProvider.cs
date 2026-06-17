using Microsoft.AspNetCore.Http;
using VendlyServer.Domain.Entities.Orders;

namespace VendlyServer.Infrastructure.Payments;

// Payment provider strategiyasi (Payme, Click). Har provider o'z redirect URL'ini quradi
// va o'z webhook wire formatini parse qilib javob beradi — yagona webhook controller
// faqat Name bo'yicha yo'naltiradi. Hamkor bunga kirmaydi (u alohida outbound broker).
public interface IPaymentProvider
{
    // Route segmenti (kichik harf): "payme" / "click".
    string Name { get; }

    string CreatePaymentUrl(Order order);

    // Provider callback'ini boshqaradi. Provider'ga xos wire formatni qaytaradi
    // (hech qachon ProblemDetails emas) — protokol xatolari javob tanasining bir qismi.
    Task<IResult> HandleWebhookAsync(HttpRequest request, CancellationToken cancellationToken);
}
