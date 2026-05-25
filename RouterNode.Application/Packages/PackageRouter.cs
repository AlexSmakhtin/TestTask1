using Microsoft.Extensions.Logging;
using RouterNode.Application.Abstractions;
using RouterNode.Domain.Routing;

namespace RouterNode.Application.Packages;

public class PackageRouter(IPackageInbox inbox,
    IPackagePassportReader passportReader,
    IOutgoingPackageWriter outgoingPackageWriter,
    IPackageArchiver packageArchiver,
    IPackageDeadLetterStore packageDeadLetterStore,
    IItemTransferNotifier itemTransferNotifier,
    IPackageRoutingPolicy routingPolicy,
    ILogger<PackageRouter> logger)
    : IPackageRouter
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
                outgoingPackageWriter.EnsureCanWrite(package, decisions);

                foreach (var decision in decisions)
                {
                    if (outgoingPackageWriter.IsAlreadyWritten(decision))
                    {
                        continue;
                    }

                    await itemTransferNotifier.NotifyAsync(decision.Item, cancellationToken);
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
                await MoveToDeadLetterAsync(package, exception, cancellationToken);
            }
        }

        return new PackageProcessingResult(packagesProcessed, itemsRouted, packagesFailed);
    }

    private async Task MoveToDeadLetterAsync(InboxPackage package, Exception reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await packageDeadLetterStore.MoveAsync(package, reason, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to move package {PackageName} to dead letter storage.", package.Name);
        }
    }
}