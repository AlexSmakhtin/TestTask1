using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RouterNode.Application.Abstractions;
using RouterNode.Domain.Routing;
using RouterNode.Infrastructure.Files;
using RouterNode.Infrastructure.Packages;
using RouterNode.Infrastructure.Telegram;

namespace RouterNode.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRouterNodeInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPackageFolderNamePolicy, SafePackageFolderNamePolicy>();
        services.AddSingleton<IPackageRoutingPolicy, PackageRoutingPolicy>();
        services.AddSingleton<IPackageFileSystemPaths, PackageFileSystemPathsHelper>();
        services.AddSingleton<IPackageInbox, FileSystemPackageInbox>();
        services.AddSingleton<IPackagePassportReader, XmlPackagePassportReader>();
        services.AddSingleton<IOutgoingPackageWriter, XmlOutgoingPackageWriter>();
        services.AddSingleton<IPackageArchiver, ZipPackageArchiver>();
        services.AddHttpClient<IItemTransferNotifier, TelegramItemTransferNotifier>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;
            client.BaseAddress = new Uri(options.ApiBaseUrl);
        });

        return services;
    }
}
