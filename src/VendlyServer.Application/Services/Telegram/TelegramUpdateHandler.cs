using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Products;
using VendlyServer.Application.Services.Products.Contracts;
using VendlyServer.Application.Services.Telegram.Contracts;

namespace VendlyServer.Application.Services.Telegram;

public sealed class TelegramUpdateHandler(
    ITelegramBotClient botClient,
    IProductService productService,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramUpdateHandler> logger) : ITelegramUpdateHandler
{
    private const int EmptyQueryCacheTimeSeconds = 5;
    private const int ResultCacheTimeSeconds = 30;
    private readonly TelegramBotOptions _options = options.Value;

    public async Task HandleAsync(TelegramUpdate update, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        if (update.InlineQuery is not null)
        {
            await HandleInlineQueryAsync(update.InlineQuery, cancellationToken);
            return;
        }

        if (update.Message is not null)
            await HandleMessageAsync(update.Message, cancellationToken);
    }

    private async Task HandleInlineQueryAsync(TelegramInlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        var query = inlineQuery.Query.Trim();

        if (query.Length < 2)
        {
            await botClient.AnswerInlineQueryAsync(inlineQuery.Id, [], EmptyQueryCacheTimeSeconds, cancellationToken);
            return;
        }

        var result = await productService.SearchAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning(
                "Telegram inline product search failed for query {Query}: {Error}",
                query,
                result.Error.Code);

            await botClient.AnswerInlineQueryAsync(inlineQuery.Id, [], EmptyQueryCacheTimeSeconds, cancellationToken);
            return;
        }

        var limit = Math.Max(1, _options.InlineResultLimit);
        var products = (result.Data ?? [])
            .Take(limit)
            .ToList();

        logger.LogInformation(
            "Telegram inline query {InlineQueryId} searched {Query} and found {ProductCount} products.",
            inlineQuery.Id,
            query,
            products.Count);

        var results = products
            .Select(BuildInlineResult)
            .ToList();

        try
        {
            await botClient.AnswerInlineQueryAsync(inlineQuery.Id, results, ResultCacheTimeSeconds, cancellationToken);
        }
        catch (HttpRequestException ex) when (results.Any(IsPhotoResult))
        {
            logger.LogWarning(
                ex,
                "Telegram photo inline result failed for query {Query}. Retrying with article results.",
                query);

            var fallbackResults = products
                .Select(BuildArticleResult)
                .ToList();

            await botClient.AnswerInlineQueryAsync(
                inlineQuery.Id,
                fallbackResults,
                EmptyQueryCacheTimeSeconds,
                cancellationToken);
        }
    }

    private async Task HandleMessageAsync(TelegramMessage message, CancellationToken cancellationToken)
    {
        if (message.Chat is null || string.IsNullOrWhiteSpace(message.Text))
            return;

        if (message.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendMessageAsync(
                message.Chat.Id,
                "Assalomu alaykum! Mahsulot qidirish uchun chatda bot username'ini yozib, keyin mahsulot nomini kiriting.",
                cancellationToken);
        }
    }

    private Dictionary<string, object?> BuildInlineResult(ProductSearchResponse product)
    {
        var imageUrl = ResolvePublicUrl(product.Images.FirstOrDefault());
        return string.IsNullOrWhiteSpace(imageUrl) || !IsTelegramFetchableImageUrl(imageUrl)
            ? BuildArticleResult(product)
            : BuildPhotoResult(product, imageUrl);
    }

    private static bool IsPhotoResult(Dictionary<string, object?> result)
    {
        return result.TryGetValue("type", out var type) &&
               string.Equals(type?.ToString(), "photo", StringComparison.Ordinal);
    }

    private static bool IsTelegramFetchableImageUrl(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme is not ("http" or "https"))
            return false;

        return !uri.IsLoopback &&
               !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    private Dictionary<string, object?> BuildPhotoResult(ProductSearchResponse product, string imageUrl)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "photo",
            ["id"] = product.Id.ToString(CultureInfo.InvariantCulture),
            ["photo_url"] = imageUrl,
            ["thumbnail_url"] = imageUrl,
            ["title"] = product.Name,
            ["description"] = BuildDescription(product),
            ["caption"] = BuildProductMessage(product),
            ["parse_mode"] = "HTML",
            ["reply_markup"] = BuildOpenProductKeyboard(product.RedirectUrl)
        };
    }

    private Dictionary<string, object?> BuildArticleResult(ProductSearchResponse product)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "article",
            ["id"] = product.Id.ToString(CultureInfo.InvariantCulture),
            ["title"] = product.Name,
            ["description"] = BuildDescription(product),
            ["input_message_content"] = new TelegramInputTextMessageContent
            {
                MessageText = BuildProductMessage(product)
            },
            ["reply_markup"] = BuildOpenProductKeyboard(product.RedirectUrl)
        };
    }

    private static string BuildDescription(ProductSearchResponse product)
    {
        var stockStatus = product.IsAvailableForSale ? "Sotuvda bor" : "Sotuvda yo'q";
        return $"{FormatPrice(product.Price)} so'm | SKUlar: {product.SkuCount} | {stockStatus}";
    }

    private static string BuildProductMessage(ProductSearchResponse product)
    {
        var stockStatus = product.IsAvailableForSale ? "Sotuvda bor" : "Sotuvda yo'q";
        return $"""
                <b>{EscapeHtml(product.Name)}</b>

                Narxi: {FormatPrice(product.Price)} so'm
                SKUlar soni: {product.SkuCount}
                Holati: {stockStatus}
                """;
    }

    private static TelegramInlineKeyboardMarkup BuildOpenProductKeyboard(string redirectUrl)
    {
        return new TelegramInlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new TelegramInlineKeyboardButton
                    {
                        Text = "Mahsulotni ochish",
                        Url = redirectUrl
                    }
                ]
            ]
        };
    }

    private string? ResolvePublicUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
            return absoluteUri.ToString();

        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return imageUrl;

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{imageUrl.TrimStart('/')}";
    }

    private static string FormatPrice(decimal price)
    {
        var format = decimal.Truncate(price) == price ? "#,0" : "#,0.##";
        return price
            .ToString(format, CultureInfo.InvariantCulture)
            .Replace(",", " ", StringComparison.Ordinal);
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }
}
