using Microsoft.Extensions.Logging;
using RouterNode.Application.Abstractions;
using RouterNode.Domain.Routing;

namespace RouterNode.Application.Packages;

public sealed class PackageRouter(IPackageInbox inbox,
    IPackagePassportReader passportReader,
    IOutgoingPackageWriter outgoingPackageWriter,
    IPackageArchiver packageArchiver,
    IItemTransferNotifier itemTransferNotifier,
    IPackageRoutingPolicy routingPolicy,
    ILogger<PackageRouter> logger)
{
    public async Task<PackageProcessingResult> ProcessReadyPackagesAsync(CancellationToken cancellationToken)
    {
        var packagesProcessed = 0;
        var itemsRouted = 0;
        var packagesFailed = 0;

        foreach (var package in inbox.GetReadyPackages())
        {
            try
            {
                var passport = await passportReader.ReadAsync(package, cancellationToken);
                var decisions = routingPolicy.Route(passport);

                foreach (var decision in decisions)
                {
                    if (outgoingPackageWriter.Exists(decision))
                    {
                        itemsRouted++;

                        continue;
                    }

                    await NotifyAsync(decision, cancellationToken);

                    await outgoingPackageWriter.WriteAsync(package, decision, cancellationToken);
                    itemsRouted++;
                }

                await packageArchiver.ArchiveAsync(package, cancellationToken);
                packagesProcessed++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                packagesFailed++;
                logger.LogError(exception, "Failed to process package {PackageName}", package.Name);
            }
        }

        return new PackageProcessingResult(packagesProcessed, itemsRouted, packagesFailed);
    }

    private async Task NotifyAsync(RoutingDecision decision, CancellationToken cancellationToken)
    {
        try
        {
            await itemTransferNotifier.NotifyAsync(decision.Item, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception,
                "Failed to send transfer notification for order {OrderId}. Routing result is kept.",
                decision.Item.OrderId);
        }
    }
}
