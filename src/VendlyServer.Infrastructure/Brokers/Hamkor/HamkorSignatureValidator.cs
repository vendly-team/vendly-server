using System.Security.Cryptography;
using System.Text;

namespace VendlyServer.Infrastructure.Brokers.Hamkor;

/// <summary>
/// Verifies the callback signature sent by Hamkorbank.
/// Two-stage SHA3-256 (uppercase hex), per the acquiring reference guide:
///   first     = UPPER(SHA3-256(key + secret + externalId))
///   signature = UPPER(SHA3-256(first + externalId))
/// </summary>
public static class HamkorSignatureValidator
{
    public static string Calculate(string key, string secret, string externalId)
    {
        var firstInput = key + secret + externalId;
        var firstHash = Convert.ToHexString(Sha3(firstInput)); // already uppercase

        var secondInput = firstHash + externalId;
        return Convert.ToHexString(Sha3(secondInput)); // already uppercase
    }

    public static bool IsValid(string key, string secret, string externalId, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        var expected = Calculate(key, secret, externalId);
        return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] Sha3(string input) =>
        SHA3_256.HashData(Encoding.UTF8.GetBytes(input));
}
