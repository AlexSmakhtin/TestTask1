using RouterNode.Domain.Entities;

namespace RouterNode.Domain.Notifications;

public interface IItemTransferNotifier
{
    Task NotifyAsync(PackageItem item, CancellationToken cancellationToken);
}
