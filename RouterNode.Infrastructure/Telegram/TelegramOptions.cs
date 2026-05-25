namespace RouterNode.Infrastructure.Telegram;

public class TelegramOptions
{
    public string ApiBaseUrl { get; set; } = null!;

    public string BotToken { get; set; } = null!;

    public string ChatId { get; set; } = null!;

    public Uri SendMessageUri => new($"{ApiBaseUrl.TrimEnd('/')}/bot{BotToken}/sendMessage", UriKind.Absolute);
}
