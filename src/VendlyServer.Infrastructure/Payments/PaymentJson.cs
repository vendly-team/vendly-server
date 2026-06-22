using System.Text.Json;
using System.Text.Json.Serialization;

namespace VendlyServer.Infrastructure.Payments;

// Webhook javoblari uchun serializer sozlamalari. Global MVC sozlamalaridan mustaqil —
// Click/Payme wire formatlari raqamli kodlar va [JsonPropertyName] orqali snake_case talab qiladi.
public static class PaymentJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
