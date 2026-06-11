using System.Text;

namespace VendlyServer.Infrastructure.Extensions;

public static class StringExtensions
{
    private static readonly Dictionary<string, string> _map = new()
    {
        // Special combinations first (two-letter Cyrillic characters)
        { "Ё", "YO" }, { "ё", "yo" },
        { "Ю", "YU" }, { "ю", "yu" },
        { "Я", "YA" }, { "я", "ya" },
        { "Ш", "SH" }, { "ш", "sh" },
        { "Ч", "CH" }, { "ч", "ch" },
        { "Ғ", "G‘" }, { "ғ", "g‘" },
        { "Ў", "O‘" }, { "ў", "o‘" },
        { "Қ", "Q" },  { "қ", "q" },
        { "Ҳ", "H" },  { "ҳ", "h" },

        // Regular letters
        { "А", "A" }, { "а", "a" },
        { "Б", "B" }, { "б", "b" },
        { "В", "V" }, { "в", "v" },
        { "Г", "G" }, { "г", "g" },
        { "Д", "D" }, { "д", "d" },
        { "Е", "E" }, { "е", "e" },
        { "Ж", "J" }, { "ж", "j" },
        { "З", "Z" }, { "з", "z" },
        { "И", "I" }, { "и", "i" },
        { "Ы", "I" }, { "ы", "i" },
        { "Й", "Y" }, { "й", "y" },
        { "К", "K" }, { "к", "k" },
        { "Л", "L" }, { "л", "l" },
        { "М", "M" }, { "м", "m" },
        { "Н", "N" }, { "н", "n" },
        { "О", "O" }, { "о", "o" },
        { "П", "P" }, { "п", "p" },
        { "Р", "R" }, { "р", "r" },
        { "С", "S" }, { "с", "s" },
        { "Т", "T" }, { "т", "t" },
        { "У", "U" }, { "у", "u" },
        { "Ф", "F" }, { "ф", "f" },
        { "Х", "X" }, { "х", "x" },
        { "Э", "E" }, { "э", "e" },
        { "Ь", ""  }, { "ь", ""  },
        { "Ъ", ""  }, { "ъ", ""  },
    };

    public static string CyrillicToLatinUz(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var sb = new StringBuilder(input.Length);

        foreach (char ch in input)
        {
            var s = ch.ToString();
            sb.Append(_map.TryGetValue(s, out var latin) ? latin : s);
        }

        return sb.ToString();
    }

    public static string LatinToCyrillicUz(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var sb = new StringBuilder(input.Length);
        var i = 0;

        while (i < input.Length)
        {
            if (i + 1 < input.Length)
            {
                var two = input.Substring(i, 2);
                var twoResult = two switch
                {
                    "SH" or "Sh" => "Ш",
                    "sh"         => "ш",
                    "CH" or "Ch" => "Ч",
                    "ch"         => "ч",
                    "G'"         => "Ғ",
                    "g'"         => "ғ",
                    "O'"         => "Ў",
                    "o'"         => "ў",
                    "Yo" or "YO" => "Ё",
                    "yo"         => "ё",
                    "Yu" or "YU" => "Ю",
                    "yu"         => "ю",
                    "Ya" or "YA" => "Я",
                    "ya"         => "я",
                    _            => null
                };

                if (twoResult is not null)
                {
                    sb.Append(twoResult);
                    i += 2;
                    continue;
                }
            }

            sb.Append(input[i].ToString() switch
            {
                "A" => "А", "a" => "а",
                "B" => "Б", "b" => "б",
                "V" => "В", "v" => "в",
                "G" => "Г", "g" => "г",
                "D" => "Д", "d" => "д",
                "E" => "Е", "e" => "е",
                "J" => "Ж", "j" => "ж",
                "Z" => "З", "z" => "з",
                "I" => "И", "i" => "и",
                "Y" => "Й", "y" => "й",
                "K" => "К", "k" => "к",
                "L" => "Л", "l" => "л",
                "M" => "М", "m" => "м",
                "N" => "Н", "n" => "н",
                "O" => "О", "o" => "о",
                "P" => "П", "p" => "п",
                "Q" => "Қ", "q" => "қ",
                "R" => "Р", "r" => "р",
                "S" => "С", "s" => "с",
                "T" => "Т", "t" => "т",
                "U" => "У", "u" => "у",
                "F" => "Ф", "f" => "ф",
                "X" => "Х", "x" => "х",
                "H" => "Ҳ", "h" => "ҳ",
                var ch => ch
            });

            i++;
        }

        return sb.ToString();
    }
}