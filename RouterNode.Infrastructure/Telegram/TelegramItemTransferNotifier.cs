using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using RouterNode.Application.Abstractions;
using RouterNode.Domain.Packages;

namespace RouterNode.Infrastructure.Telegram;

public class TelegramItemTransferNotifier(HttpClient httpClient, IOptions<TelegramOptions> options)
    : IItemTransferNotifier
{
    private TelegramOptions Options { get; } = options.Value;

    public async Task NotifyAsync(PackageItem item, CancellationToken cancellationToken)
    {
        var response = await httpClient
            .PostAsJsonAsync(Options.SendMessageUri, new TelegramSendMessageRequest(item, Options.ChatId),
                cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
