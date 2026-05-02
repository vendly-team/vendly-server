using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Products;
using VendlyServer.Application.Services.Products.Contracts;
using VendlyServer.Application.Services.Telegram.Contracts;

namespace VendlyServer.Application.Services.Telegram;

public sealed class TelegramUpdateHandler(
    ITelegramBotClient botClient,
    ITelegramImageUrlValidator imageUrlValidator,
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

        var results = new List<Dictionary<string, object?>>();
        foreach (var product in products)
            results.Add(await BuildInlineResultAsync(product, cancellationToken));

        await botClient.AnswerInlineQueryAsync(inlineQuery.Id, results, ResultCacheTimeSeconds, cancellationToken);
    }

    private async Task HandleMessageAsync(TelegramMessage message, CancellationToken cancellationToken)
    {
        if (message.Chat is null || string.IsNullOrWhiteSpace(message.Text))
            return;

        if (message.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendMessageAsync(
                message.Chat.Id,
                "Assalomu alaykum! 👋\n\n🔎 Mahsulot qidirish uchun istalgan chatda bot username'ini yozing va yoniga mahsulot nomini kiriting.\n\nMasalan: <code>@bot kir yuvish mashinasi</code>",
                cancellationToken);
        }
    }

    private async Task<Dictionary<string, object?>> BuildInlineResultAsync(
        ProductSearchResponse product,
        CancellationToken cancellationToken)
    {
        var imageUrl = await SelectTelegramImageUrlAsync(product.Images, cancellationToken);
        return BuildArticleResult(product, imageUrl);
    }

    private async Task<string?> SelectTelegramImageUrlAsync(
        IReadOnlyCollection<string> imageUrls,
        CancellationToken cancellationToken)
    {
        string? firstFetchableImageUrl = null;

        foreach (var imageUrl in imageUrls)
        {
            var resolvedImageUrl = ResolvePublicUrl(imageUrl);
            if (string.IsNullOrWhiteSpace(resolvedImageUrl) ||
                !IsTelegramFetchableImageUrl(resolvedImageUrl))
                continue;

            firstFetchableImageUrl ??= resolvedImageUrl;

            if (await imageUrlValidator.IsValidThumbnailAsync(resolvedImageUrl, cancellationToken))
                return resolvedImageUrl;
        }

        return firstFetchableImageUrl;
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

    private Dictionary<string, object?> BuildArticleResult(ProductSearchResponse product, string? imageUrl = null)
    {
        var result = new Dictionary<string, object?>
        {
            ["type"] = "article",
            ["id"] = product.Id.ToString(CultureInfo.InvariantCulture),
            ["title"] = $"🛍️ {product.Name}",
            ["description"] = BuildDescription(product),
            ["input_message_content"] = new TelegramInputTextMessageContent
            {
                MessageText = BuildProductMessage(product, imageUrl),
                DisableWebPagePreview = string.IsNullOrWhiteSpace(imageUrl),
                LinkPreviewOptions = string.IsNullOrWhiteSpace(imageUrl)
                    ? null
                    : new TelegramLinkPreviewOptions { Url = imageUrl }
            },
            ["reply_markup"] = BuildOpenProductKeyboard(product.RedirectUrl)
        };

        if (!string.IsNullOrWhiteSpace(imageUrl) && IsTelegramFetchableImageUrl(imageUrl))
        {
            result["thumbnail_url"] = imageUrl;
            result["thumbnail_width"] = 160;
            result["thumbnail_height"] = 160;
        }

        return result;
    }

    private static string BuildDescription(ProductSearchResponse product)
    {
        var stockStatus = product.IsAvailableForSale ? "✅ Sotuvda bor" : "⛔ Hozircha sotuvda yo'q";
        return $"💰 {FormatPrice(product.Price)} so'm • 📦 {product.SkuCount} variant • {stockStatus}";
    }

    private static string BuildProductMessage(ProductSearchResponse product, string? imageUrl)
    {
        var stockStatus = product.IsAvailableForSale ? "✅ Sotuvda bor" : "⛔ Hozircha sotuvda yo'q";
        var imagePreviewLink = string.IsNullOrWhiteSpace(imageUrl)
            ? string.Empty
            : $"<a href=\"{EscapeHtml(imageUrl)}\">&#8205;</a>\n";

        return $"""
                {imagePreviewLink}🛍️ <b>{EscapeHtml(product.Name)}</b>

                💰 Narxi: {FormatPrice(product.Price)} so'm
                📦 Variantlar: {product.SkuCount} ta
                {stockStatus}

                👇 Batafsil ko'rish uchun tugmani bosing.
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
                        Text = "🛍️ Mahsulotni ochish",
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
