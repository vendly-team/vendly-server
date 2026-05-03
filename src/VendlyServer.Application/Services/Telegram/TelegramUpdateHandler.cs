using System.Globalization;
using System.Net;
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
    private static readonly string[] HappyCalmEmojis = ["👍", "❤", "🥰", "👏", "🎉", "🙏", "👌", "🤝", "🤗", "😇"];
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
            results.Add(await BuildInlineResultAsync(product, inlineQuery.From?.Id, cancellationToken));

        await botClient.AnswerInlineQueryAsync(inlineQuery.Id, results, ResultCacheTimeSeconds, cancellationToken);
    }

    private async Task HandleMessageAsync(TelegramMessage message, CancellationToken cancellationToken)
    {
        if (message.Chat is null || string.IsNullOrWhiteSpace(message.Text))
            return;

        if (message.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            var emoji = HappyCalmEmojis[Random.Shared.Next(HappyCalmEmojis.Length)];
            await TrySetStartReactionAsync(message, emoji, cancellationToken);

            await botClient.SendMessageAsync(
                message.Chat.Id,
                BuildStartMessage(emoji),
                cancellationToken,
                BuildSearchKeyboard(message),
                message.MessageId > 0 ? message.MessageId : null);

            try
            {
                await botClient.SendDocumentAsync(
                    message.Chat.Id,
                    BuildWelcomeGifUrl(),
                    "🎁 Xush kelibsiz!",
                    null,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "Failed to send Telegram welcome document to chat {ChatId}.",
                    message.Chat.Id);

                await TrySendWelcomeAnimationAsync(message.Chat.Id, cancellationToken);
            }
        }
    }

    private async Task TrySetStartReactionAsync(TelegramMessage message, string emoji, CancellationToken cancellationToken)
    {
        if (message.Chat is null || message.MessageId <= 0)
            return;

        try
        {
            await botClient.SetMessageReactionAsync(message.Chat.Id, message.MessageId, emoji, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to set Telegram start reaction for chat {ChatId} and message {MessageId}.",
                message.Chat.Id,
                message.MessageId);
        }
    }

    private async Task TrySendWelcomeAnimationAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.SendAnimationAsync(
                chatId,
                BuildWelcomeGifUrl(),
                "🎁 Xush kelibsiz!",
                null,
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to send Telegram welcome animation to chat {ChatId}.",
                chatId);
        }
    }

    private static string BuildStartMessage(string emoji)
    {
        return $"""
                {emoji} <b>Assalomu alaykum!</b>

                Bu Opto'ning rasmiy boti.

                Mahsulotlarni tez qidirishingiz mumkin.

                Inline qidiruv: istalgan chatda <code>@optouzbot</code> va mahsulot nomini yozing.
                """;
    }

    private static TelegramInlineKeyboardMarkup BuildSearchKeyboard(TelegramMessage message)
    {
        return new TelegramInlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new TelegramInlineKeyboardButton
                    {
                        Text = "🔎 Mahsulot qidirish",
                        Url = BuildSearchDeepLink(message)
                    }
                ]
            ]
        };
    }

    private static string BuildSearchDeepLink(TelegramMessage message)
    {
        var username = (message.From?.Username ?? message.Chat?.Username)?.Trim().TrimStart('@');
        var encodedText = Uri.EscapeDataString("@optouzbot ab");

        if (!string.IsNullOrWhiteSpace(username))
        {
            var encodedUsername = Uri.EscapeDataString(username);
            return $"https://t.me/{encodedUsername}?text={encodedText}".Trim();
        }

        return $"https://t.me/share/url?text={encodedText}".Trim();
    }

    private string BuildWelcomeGifUrl()
    {
        return $"{_options.PublicBaseUrl.TrimEnd('/')}/tgbot-welcome.gif";
    }

    private async Task<Dictionary<string, object?>> BuildInlineResultAsync(
        ProductSearchResponse product,
        long? refChatId,
        CancellationToken cancellationToken)
    {
        var imageUrl = await SelectTelegramImageUrlAsync(product.Images, cancellationToken);
        return BuildArticleResult(product, imageUrl, refChatId);
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

        if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            return false;

        if (IPAddress.TryParse(uri.Host, out var ip) && IsPrivateOrReservedIp(ip))
            return false;

        return true;
    }

    private static bool IsPrivateOrReservedIp(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return ip.IsIPv6LinkLocal
            || ip.IsIPv6SiteLocal
            || (bytes.Length == 4 && (
                bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254)));
    }

    private Dictionary<string, object?> BuildArticleResult(
        ProductSearchResponse product,
        string? imageUrl = null,
        long? refChatId = null)
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
            ["reply_markup"] = BuildOpenProductKeyboard(product.RedirectUrl, refChatId)
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

    private static TelegramInlineKeyboardMarkup BuildOpenProductKeyboard(string redirectUrl, long? refChatId)
    {
        return new TelegramInlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new TelegramInlineKeyboardButton
                    {
                        Text = "🛍️ Mahsulotni ochish",
                        Url = BuildProductRedirectUrl(redirectUrl, refChatId)
                    }
                ]
            ]
        };
    }

    private static string BuildProductRedirectUrl(string redirectUrl, long? refChatId)
    {
        if (!refChatId.HasValue)
            return redirectUrl;

        var separator = redirectUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{redirectUrl}{separator}ref={refChatId.Value}";
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
