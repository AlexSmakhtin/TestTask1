using RouterNode.Application.Packages;
using RouterNode.HostedServices;
using RouterNode.Infrastructure;
using RouterNode.Infrastructure.Files;
using RouterNode.Infrastructure.Telegram;
using RouterNode.Options;

namespace RouterNode;

public class Program
{
    public static async Task Main()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services
            .AddOptions<PackageRouterOptions>()
            .Bind(builder.Configuration.GetSection("PackageRouter"))
            .Validate(options => options.PollingIntervalSeconds > 0,
                "PackageRouter:PollingIntervalSeconds must be greater than zero.")
            .ValidateOnStart();
        builder.Services
            .AddOptions<FileSystemPackageOptions>()
            .Bind(builder.Configuration.GetSection("FileSystemPackageStorage"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.InboxPath), "FileSystemPackageStorage:InboxPath is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.OutboxPath),
                "FileSystemPackageStorage:OutboxPath is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ProcessingPath),
                "FileSystemPackageStorage:ProcessingPath is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ArchivePath),
                "FileSystemPackageStorage:ArchivePath is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SchemaPath),
                "FileSystemPackageStorage:SchemaPath is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.PassportFileName),
                "FileSystemPackageStorage:PassportFileName is required.")
            .ValidateOnStart();
        builder.Services
            .AddOptions<TelegramOptions>()
            .Bind(builder.Configuration.GetSection("Telegram"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiBaseUrl), "Telegram:ApiBaseUrl is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.BotToken), "Telegram:BotToken is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ChatId), "Telegram:ChatId is required.")
            .ValidateOnStart();
        builder.Services.AddSingleton<IPackageRouter, PackageRouter>();
        builder.Services.AddRouterNodeInfrastructure();
        builder.Services.AddHostedService<PackageRouterHostedService>();

        var app = builder.Build();

        await app.RunAsync();
    }
}
