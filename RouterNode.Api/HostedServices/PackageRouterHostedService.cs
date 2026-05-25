using Microsoft.Extensions.Options;
using RouterNode.Domain.Routing;
using RouterNode.Infrastructure.Packages;

namespace RouterNode.HostedServices;

public sealed class PackageRouterHostedService(IPackageRouter packageRouter,
    IOptions<PackageRouterOptions> options,
    ILogger<PackageRouterHostedService> logger)
    : BackgroundService
{
    private PackageRouterOptions Options { get; } = options.Value;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var (packagesProcessed, itemsRouted, packagesFailed) = await packageRouter
                    .ProcessReadyPackagesAsync(cancellationToken);

                if (packagesProcessed > 0 || packagesFailed > 0)
                {
                    logger.LogInformation(
                        "Routing iteration completed. Packages processed: {PackagesProcessed}, items routed: {ItemsRouted}, packages failed: {PackagesFailed}",
                        packagesProcessed, itemsRouted, packagesFailed);
                }

                await Task.Delay(TimeSpan.FromSeconds(Options.PollingIntervalSeconds), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Routing iteration failed.");
            }
        }
    }
}