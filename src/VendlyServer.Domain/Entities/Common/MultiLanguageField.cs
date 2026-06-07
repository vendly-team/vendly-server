using System.Text.Json;
using System.Text.Json.Serialization;

namespace VendlyServer.Domain.Entities.Common;

public class MultiLanguageField
{
    private bool Equals(MultiLanguageField other)
    {
        return this.Uz == other.Uz &&
               this.Ru == other.Ru &&
               this.En == other.En &&
               this.Cyrl == other.Cyrl;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        
        return this.Equals((MultiLanguageField)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Uz, this.Ru, this.En, this.Cyrl);
    }

    [JsonPropertyName("uz")]
    public string? Uz { get; set; }

    [JsonPropertyName("ru")]
    public string? Ru { get; set; }

    [JsonPropertyName("en")]
    public string? En { get; set; }

    [JsonPropertyName("cyrl")]
    public string? Cyrl { get; set; }

    public static implicit operator MultiLanguageField(string data) => new MultiLanguageField()
    {
        Ru = data, Uz = data, En = data, Cyrl = data
    };

    public static bool operator ==(MultiLanguageField a, string b)
    {
        return a.Ru == b ||
               a.En == b ||
               a.Uz == b ||
               a.Cyrl == b;
    }

    public static bool operator !=(MultiLanguageField a, string b)
    {
        return a.Ru != b &&
               a.En != b &&
               a.Uz != b &&
               a.Cyrl != b;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
