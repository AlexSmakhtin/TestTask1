namespace RouterNode.Infrastructure.Telegram;

public class TelegramOptions
{
    public string ApiBaseUrl { get; set; } = null!;

    public string BotToken { get; set; } = null!;

    public string ChatId { get; set; } = null!;

    public string Url => $"bot{BotToken}/sendMessage";
}
