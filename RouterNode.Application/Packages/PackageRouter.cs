using Microsoft.Extensions.Logging;
using RouterNode.Domain.Entities;
using RouterNode.Domain.Notifications;
using RouterNode.Domain.Packages;
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
                var decisions = routingPolicy.GetRouteDecisions(passport);
                outgoingPackageWriter.EnsureCanWrite(package, decisions);
                var decisionsToWrite = decisions
                    .Where(decision => !outgoingPackageWriter.IsAlreadyWritten(decision))
                    .ToArray();
                var drafts = new List<OutgoingPackageDraft>(decisionsToWrite.Length);
                var publishedDrafts = new List<OutgoingPackageDraft>(decisionsToWrite.Length);

                try
                {
                    foreach (var decision in decisionsToWrite)
                    {
                        drafts.Add(await outgoingPackageWriter.PrepareAsync(package, decision, cancellationToken));
                    }

                    foreach (var draft in drafts)
                    {
                        await outgoingPackageWriter.PublishAsync(draft, cancellationToken);
                        publishedDrafts.Add(draft);
                    }

                    await packageArchiver.ArchiveAsync(package, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    RollbackOutgoingPackage(drafts, publishedDrafts);

                    throw;
                }

                packagesProcessed++;
                itemsRouted += decisionsToWrite.Length;

                foreach (var decision in decisionsToWrite)
                {
                    await NotifyAsync(decision, cancellationToken);
                }
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

    private async Task NotifyAsync(RoutingDecision decision, CancellationToken cancellationToken)
    {
        try
        {
            await itemTransferNotifier.NotifyAsync(decision.Item, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to notify item {OrderId} transfer.", decision.Item.OrderId);
        }
    }

    private void RollbackOutgoingPackage(IReadOnlyCollection<OutgoingPackageDraft> drafts,
        IReadOnlyCollection<OutgoingPackageDraft> publishedDrafts)
    {
        foreach (var draft in publishedDrafts.Reverse())
        {
            try
            {
                outgoingPackageWriter.RemovePublished(draft);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Failed to remove published package item {OrderId}.",
                    draft.Decision.Item.OrderId);
            }
        }

        foreach (var draft in drafts.Where(draft => !publishedDrafts.Contains(draft)).Reverse())
        {
            try
            {
                outgoingPackageWriter.RemoveTemporary(draft);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Failed to discard package item draft {OrderId}.",
                    draft.Decision.Item.OrderId);
            }
        }
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
