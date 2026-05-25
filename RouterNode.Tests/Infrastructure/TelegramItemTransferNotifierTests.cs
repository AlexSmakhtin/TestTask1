using System.Net;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using RouterNode.Domain.Entities;
using RouterNode.Infrastructure.Telegram;
using Xunit;

namespace RouterNode.Tests.Infrastructure;

public sealed class TelegramItemTransferNotifierTests
{
    [Fact]
    public async Task NotifyAsync_WhenBotTokenContainsColon_SendsRequestToTelegramApiUrl()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        using var httpClient = new HttpClient(handler.Object);
        var options = Options.Create(new TelegramOptions
        {
            ApiBaseUrl = "https://api.telegram.org/",
            BotToken = "123456:ABCDEF",
            ChatId = "42"
        });
        var notifier = new TelegramItemTransferNotifier(httpClient, options);
        var item = new PackageItem("order-1", "file.txt", "Title", null, 1, 10m);

        // Act
        await notifier.NotifyAsync(item, CancellationToken.None);

        // Assert
        handler
            .Protected()
            .Verify("SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Post
                    && request.RequestUri == new Uri("https://api.telegram.org/bot123456:ABCDEF/sendMessage")),
                ItExpr.IsAny<CancellationToken>());
    }
}
