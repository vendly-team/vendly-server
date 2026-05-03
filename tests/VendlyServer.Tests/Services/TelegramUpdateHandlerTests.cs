using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Products;
using VendlyServer.Application.Services.Products.Contracts;
using VendlyServer.Application.Services.Telegram;
using VendlyServer.Application.Services.Telegram.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Tests.Services;

public class TelegramUpdateHandlerTests
{
    private readonly FakeTelegramBotClient _botClient = new();
    private readonly FakeProductService _productService = new();
    private readonly TelegramUpdateHandler _handler;

    public TelegramUpdateHandlerTests()
    {
        _handler = new TelegramUpdateHandler(
            _botClient,
            new FakeTelegramImageUrlValidator(),
            _productService,
            Options.Create(new TelegramBotOptions
            {
                Enabled = true,
                PublicBaseUrl = "https://api.vendly.uz",
                InlineResultLimit = 10
            }),
            NullLogger<TelegramUpdateHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_UsesFirstValidTelegramThumbnail_WhenFirstImageIsTooLarge()
    {
        var largeImage = "https://files.vendly.uz/large.png";
        var validImage = "https://files.vendly.uz/valid.png";
        _productService.SearchResult = Result<List<ProductSearchResponse>>.Success(
        [
            new(3, "Samsung Phone", 16_499_000m, 2, [largeImage, validImage], true, "https://vendly.uz/product/samsung-phone-3")
        ]);

        var handler = new TelegramUpdateHandler(
            _botClient,
            new FakeTelegramImageUrlValidator([validImage]),
            _productService,
            Options.Create(new TelegramBotOptions
            {
                Enabled = true,
                PublicBaseUrl = "https://api.vendly.uz",
                InlineResultLimit = 10
            }),
            NullLogger<TelegramUpdateHandler>.Instance);

        await handler.HandleAsync(new TelegramUpdate
        {
            InlineQuery = new TelegramInlineQuery { Id = "query-4", Query = "samsung" }
        });

        var result = Assert.Single(_botClient.AnsweredResults);
        Assert.Equal(validImage, result["thumbnail_url"]);
        var messageContent = Assert.IsType<TelegramInputTextMessageContent>(result["input_message_content"]);
        Assert.Equal(validImage, messageContent.LinkPreviewOptions!.Url);
        Assert.DoesNotContain(largeImage, messageContent.MessageText);
    }

    [Fact]
    public async Task HandleAsync_MapsProductWithImage_ToArticleInlineResultWithThumbnail()
    {
        _productService.SearchResult = Result<List<ProductSearchResponse>>.Success(
        [
            new(1, "Samsung TV", 2_500_000m, 4, ["https://files.vendly.uz/tv.jpg"], true, "https://vendly.uz/product/samsung-tv-1")
        ]);

        await _handler.HandleAsync(new TelegramUpdate
        {
            InlineQuery = new TelegramInlineQuery
            {
                Id = "query-1",
                Query = "samsung",
                From = new TelegramUser { Id = 777 }
            }
        });

        Assert.Equal("query-1", _botClient.AnsweredInlineQueryId);
        var result = Assert.Single(_botClient.AnsweredResults);
        Assert.Equal("article", result["type"]);
        Assert.Equal("🛍️ Samsung TV", result["title"]);
        Assert.Contains("2 500 000", result["description"]!.ToString());
        Assert.Contains("Sotuvda bor", result["description"]!.ToString());
        Assert.Equal("https://files.vendly.uz/tv.jpg", result["thumbnail_url"]);
        Assert.Contains("Mahsulotni ochish", result["reply_markup"]!.ToString());
        Assert.Contains("ref=777", result["reply_markup"]!.ToString());
        Assert.Contains("input_message_content", string.Join(',', result.Keys));
    }

    [Fact]
    public async Task HandleAsync_MapsProductWithoutImage_ToArticleInlineResult()
    {
        _productService.SearchResult = Result<List<ProductSearchResponse>>.Success(
        [
            new(2, "Bosch Washer", 1_200_000m, 2, [], false, "https://vendly.uz/product/bosch-washer-2")
        ]);

        await _handler.HandleAsync(new TelegramUpdate
        {
            InlineQuery = new TelegramInlineQuery { Id = "query-2", Query = "bosch" }
        });

        var result = Assert.Single(_botClient.AnsweredResults);
        Assert.Equal("article", result["type"]);
        Assert.Equal("🛍️ Bosch Washer", result["title"]);
        Assert.Contains("Sotuvda yo'q", result["description"]!.ToString());
        Assert.Contains("input_message_content", string.Join(',', result.Keys));
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyResults_ForShortInlineQuery()
    {
        await _handler.HandleAsync(new TelegramUpdate
        {
            InlineQuery = new TelegramInlineQuery { Id = "query-3", Query = "a" }
        });

        Assert.Empty(_botClient.AnsweredResults);
        Assert.Equal(0, _productService.SearchCalls);
    }

    [Fact]
    public async Task HandleAsync_RepliesToStartCommand_InUzbek()
    {
        await _handler.HandleAsync(new TelegramUpdate
        {
            Message = new TelegramMessage
            {
                Text = "/start",
                From = new TelegramUser { Id = 123, Username = "timur_test" },
                Chat = new TelegramChat { Id = 123, Username = "timur_test" }
            }
        });

        var sentDocument = Assert.Single(_botClient.SentDocuments);
        Assert.Equal(123, sentDocument.ChatId);
        Assert.Equal("https://api.vendly.uz/tgbot-welcome.gif", sentDocument.DocumentUrl);
        Assert.Contains("Assalomu alaykum", sentDocument.Caption);
        Assert.Contains("Saved Messages", sentDocument.Caption);
        Assert.Contains("https://t.me/timur_test?text=%40optouzbot%20ab", sentDocument.Caption);
        Assert.Null(sentDocument.ReplyMarkup);
        Assert.Empty(_botClient.SentAnimations);
    }

    private sealed class FakeTelegramBotClient : ITelegramBotClient
    {
        public string? AnsweredInlineQueryId { get; private set; }
        public List<Dictionary<string, object?>> AnsweredResults { get; private set; } = [];
        public List<(long ChatId, string Text)> SentMessages { get; } = [];
        public List<(long ChatId, string AnimationUrl, string Caption, TelegramInlineKeyboardMarkup? ReplyMarkup)> SentAnimations { get; } = [];
        public List<(long ChatId, string DocumentUrl, string Caption, TelegramInlineKeyboardMarkup? ReplyMarkup)> SentDocuments { get; } = [];

        public Task SetWebhookAsync(string url, string secretToken, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
        {
            SentMessages.Add((chatId, text));
            return Task.CompletedTask;
        }

        public Task SendAnimationAsync(
            long chatId,
            string animationUrl,
            string caption,
            TelegramInlineKeyboardMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            SentAnimations.Add((chatId, animationUrl, caption, replyMarkup));
            return Task.CompletedTask;
        }

        public Task SendDocumentAsync(
            long chatId,
            string documentUrl,
            string caption,
            TelegramInlineKeyboardMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            SentDocuments.Add((chatId, documentUrl, caption, replyMarkup));
            return Task.CompletedTask;
        }

        public Task AnswerInlineQueryAsync(
            string inlineQueryId,
            IReadOnlyList<Dictionary<string, object?>> results,
            int cacheTimeSeconds,
            CancellationToken cancellationToken = default)
        {
            AnsweredInlineQueryId = inlineQueryId;
            AnsweredResults = results.ToList();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTelegramImageUrlValidator(IReadOnlyCollection<string>? validUrls = null) : ITelegramImageUrlValidator
    {
        private readonly IReadOnlyCollection<string>? _validUrls = validUrls;

        public Task<bool> IsValidThumbnailAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_validUrls is null || _validUrls.Contains(imageUrl));
        }
    }

    private sealed class FakeProductService : IProductService
    {
        public int SearchCalls { get; private set; }
        public Result<List<ProductSearchResponse>> SearchResult { get; set; } = Result<List<ProductSearchResponse>>.Success([]);

        public Task<Result<List<ProductSearchResponse>>> SearchAsync(string query, CancellationToken ct = default)
        {
            SearchCalls++;
            return Task.FromResult(SearchResult);
        }

        public Task<Result<List<ProductListResponse>>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<ProductAdminDetailResponse>> GetByIdAsync(long id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<long>> CreateAsync(CreateProductRequest request, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> UpdateAsync(long id, UpdateProductRequest request, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> DeleteAsync(long id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> ToggleActiveAsync(long id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> AddVariantTypeAsync(long productId, CreateVariantTypeRequest request, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> DeleteVariantTypeAsync(long typeId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> AddVariantOptionAsync(long typeId, CreateVariantOptionRequest request, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> DeleteVariantOptionAsync(long optionId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> RegenerateVariantsAsync(long productId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> BulkUpdateVariantsAsync(long productId, BulkUpdateVariantsRequest request, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> UpdateVariantAsync(long variantId, UpdateVariantRequest request, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> DeleteVariantAsync(long variantId, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
