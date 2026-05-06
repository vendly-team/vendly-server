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
                MessageId = 456,
                Text = "/start",
                From = new TelegramUser { Id = 123, Username = "timur_test" },
                Chat = new TelegramChat { Id = 123, Username = "timur_test" }
            }
        });

        var reaction = Assert.Single(_botClient.SentReactions);
        Assert.Equal(123, reaction.ChatId);
        Assert.Equal(456, reaction.MessageId);
        Assert.NotEmpty(reaction.Emoji);

        var sentMessage = Assert.Single(_botClient.SentMessages);
        Assert.Equal(123, sentMessage.ChatId);
        Assert.Contains("Assalomu alaykum", sentMessage.Text);
        Assert.Contains("Opto'ning rasmiy boti", sentMessage.Text);
        Assert.Contains("Inline qidiruv", sentMessage.Text);
        Assert.DoesNotContain("Vendly", sentMessage.Text);
        Assert.DoesNotContain("Saved Messages", sentMessage.Text);
        Assert.Equal(456, sentMessage.ReplyToMessageId);
        Assert.Contains("🔎 Mahsulot qidirish", sentMessage.ReplyMarkup!.ToString());
        Assert.Contains("https://t.me/timur_test?text=%40optouzbot%20ab", sentMessage.ReplyMarkup.ToString());

    }

    [Fact]
    public async Task HandleAsync_SendsFallbackStartMessage_WhenRichStartMessageFails()
    {
        _botClient.ShouldFailRichMessageSend = true;

        await _handler.HandleAsync(new TelegramUpdate
        {
            Message = new TelegramMessage
            {
                MessageId = 456,
                Text = "/start",
                From = new TelegramUser { Id = 123, Username = "timur_test" },
                Chat = new TelegramChat { Id = 123, Username = "timur_test" }
            }
        });

        Assert.Equal(2, _botClient.SendMessageAttempts);
        var sentMessage = Assert.Single(_botClient.SentMessages);
        Assert.Contains("Assalomu alaykum", sentMessage.Text);
        Assert.Contains("Opto'ning rasmiy boti", sentMessage.Text);
        Assert.Null(sentMessage.ReplyMarkup);
        Assert.Null(sentMessage.ReplyToMessageId);
    }

    private sealed class FakeTelegramBotClient : ITelegramBotClient
    {
        public string? AnsweredInlineQueryId { get; private set; }
        public bool ShouldFailRichMessageSend { get; set; }
        public int SendMessageAttempts { get; private set; }
        public List<Dictionary<string, object?>> AnsweredResults { get; private set; } = [];
        public List<(long ChatId, string Text, TelegramInlineKeyboardMarkup? ReplyMarkup, long? ReplyToMessageId)> SentMessages { get; } = [];
        public List<(long ChatId, long MessageId, string Emoji)> SentReactions { get; } = [];

        public Task SetWebhookAsync(string url, string secretToken, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendMessageAsync(
            long chatId,
            string text,
            CancellationToken cancellationToken = default,
            TelegramInlineKeyboardMarkup? replyMarkup = null,
            long? replyToMessageId = null)
        {
            SendMessageAttempts++;
            if (ShouldFailRichMessageSend && replyMarkup is not null)
                throw new HttpRequestException("Telegram rejected rich message.");

            SentMessages.Add((chatId, text, replyMarkup, replyToMessageId));
            return Task.CompletedTask;
        }

        public Task SetMessageReactionAsync(
            long chatId,
            long messageId,
            string emoji,
            CancellationToken cancellationToken = default)
        {
            SentReactions.Add((chatId, messageId, emoji));
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
