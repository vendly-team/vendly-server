using System.Security.Cryptography;
using System.Text;

namespace VendlyServer.Infrastructure.Payments.Click;

public static class ClickMd5Helper
{
    // Sign source'ni Click SHOP API spec bo'yicha quradi:
    // click_trans_id + service_id + secret_key + merchant_trans_id +
    // (merchant_prepare_id, faqat action=1) + amount + action + sign_time.
    // Barcha qiymatlar form'da kelganidek aynan ishlatiladi.
    public static string BuildSignSource(
        string clickTransId,
        string serviceId,
        string secretKey,
        string merchantTransId,
        string? merchantPrepareId,
        string amount,
        string action,
        string signTime)
    {
        var sb = new StringBuilder()
            .Append(clickTransId)
            .Append(serviceId)
            .Append(secretKey)
            .Append(merchantTransId);

        if (action == "1")
            sb.Append(merchantPrepareId);

        return sb.Append(amount).Append(action).Append(signTime).ToString();
    }

    public static bool Verify(string signSource, string signString)
    {
        var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(signSource)));
        return string.Equals(hash, signString, StringComparison.OrdinalIgnoreCase);
    }
}
