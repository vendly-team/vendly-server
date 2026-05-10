using System.Text;
using System.Text.RegularExpressions;

namespace VendlyServer.Domain.Utils;

public static partial class SlugHelper
{
    private static readonly Dictionary<char, string> CyrillicMap = new()
    {
        ['А'] = "a",  ['а'] = "a",
        ['Б'] = "b",  ['б'] = "b",
        ['В'] = "v",  ['в'] = "v",
        ['Г'] = "g",  ['г'] = "g",
        ['Д'] = "d",  ['д'] = "d",
        ['Е'] = "e",  ['е'] = "e",
        ['Ё'] = "yo", ['ё'] = "yo",
        ['Ж'] = "j",  ['ж'] = "j",
        ['З'] = "z",  ['з'] = "z",
        ['И'] = "i",  ['и'] = "i",
        ['Й'] = "y",  ['й'] = "y",
        ['К'] = "k",  ['к'] = "k",
        ['Л'] = "l",  ['л'] = "l",
        ['М'] = "m",  ['м'] = "m",
        ['Н'] = "n",  ['н'] = "n",
        ['О'] = "o",  ['о'] = "o",
        ['П'] = "p",  ['п'] = "p",
        ['Р'] = "r",  ['р'] = "r",
        ['С'] = "s",  ['с'] = "s",
        ['Т'] = "t",  ['т'] = "t",
        ['У'] = "u",  ['у'] = "u",
        ['Ф'] = "f",  ['ф'] = "f",
        ['Х'] = "x",  ['х'] = "x",
        ['Ц'] = "ts", ['ц'] = "ts",
        ['Ч'] = "ch", ['ч'] = "ch",
        ['Ш'] = "sh", ['ш'] = "sh",
        ['Щ'] = "sh", ['щ'] = "sh",
        ['Ъ'] = "",   ['ъ'] = "",
        ['Ь'] = "",   ['ь'] = "",
        ['Э'] = "e",  ['э'] = "e",
        ['Ю'] = "yu", ['ю'] = "yu",
        ['Я'] = "ya", ['я'] = "ya",
        // Uzbek-specific
        ['Ғ'] = "g",  ['ғ'] = "g",
        ['Қ'] = "q",  ['қ'] = "q",
        ['Ҳ'] = "h",  ['ҳ'] = "h",
        ['Ҷ'] = "j",  ['ҷ'] = "j",
        ['Ў'] = "o",  ['ў'] = "o",
    };

    public static string ToSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var sb = new StringBuilder(value.Length * 2);
        foreach (var ch in value)
        {
            if (CyrillicMap.TryGetValue(ch, out var latin))
                sb.Append(latin);
            else
                sb.Append(ch);
        }

        var result = sb.ToString().ToLowerInvariant();
        result = NonAlphanumericRegex().Replace(result, "-");
        result = MultiDashRegex().Replace(result, "-");
        result = result.Trim('-');
        return result;
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultiDashRegex();
}
