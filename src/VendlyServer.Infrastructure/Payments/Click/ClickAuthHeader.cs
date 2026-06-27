using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace VendlyServer.Infrastructure.Payments.Click;

// Click Merchant API v2 Auth headerini tuzadi.
// Format: "Auth: {merchant_user_id}:{sha1(timestamp + secret_key)}:{timestamp}"
// timestamp — Unix soniyasi (10 raqam).
public static class ClickAuthHeader
{
    public static string Build(string merchantUserId, string secretKey, DateTimeOffset? now = null)
    {
        var ts = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var digest = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(ts + secretKey))).ToLowerInvariant();
        return $"{merchantUserId}:{digest}:{ts}";
    }
}
