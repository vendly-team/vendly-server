using System.Text.Json;
using System.Text.Json.Serialization;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Api.Converters;

public class MultiLanguageFieldConverter(IHttpContextAccessor contextAccessor) : JsonConverter<MultiLanguageField>
{
    public override MultiLanguageField Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var mlf = Activator.CreateInstance(typeToConvert);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var propName = reader.GetString();
            
            var prop = typeToConvert.GetProperties()
                .FirstOrDefault(x => x.Name.Equals(propName, StringComparison.InvariantCultureIgnoreCase));
            
            reader.Read();
            
            if (prop is not null)
                prop.SetValue(mlf, reader.GetString());
        }

        return (MultiLanguageField)mlf!;
    }

    public override void Write(Utf8JsonWriter writer, MultiLanguageField value, JsonSerializerOptions options)
    {
        var langCode = contextAccessor.HttpContext!.Request.Headers["Accept-Language"].FirstOrDefault();

        switch (langCode)
        {
            case "UZ":
                writer.WriteStringValue(value.Uz);
                break;
            case "RU":
                writer.WriteStringValue(value.Ru);
                break;
            case "EN":
                writer.WriteStringValue(value.En);
                break;
            case "UZ-CYRL":
                writer.WriteStringValue(value.Cyrl);
                break;
            default:
                writer.WriteRawValue(value.ToString(), true);
                break;
        }
    }
}
