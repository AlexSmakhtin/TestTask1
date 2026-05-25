using System.Text.Json.Serialization;
using RouterNode.Domain.Packages;

namespace RouterNode.Infrastructure.Telegram;

public record TelegramSendMessageRequest
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; }

    public string Text { get; }

    public TelegramSendMessageRequest(PackageItem package, string chatId)
    {
        Text =
            $"Transferred item '{package.Title}' ({package.OrderId}) to {package.RouteChannel}. Price: {package.Price}.";
        ChatId = chatId;
    }
}