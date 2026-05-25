using RouterNode.Domain.Packages;

namespace RouterNode.Application.Abstractions;

public interface IItemTransferNotifier
{
    Task NotifyAsync(PackageItem item, CancellationToken cancellationToken);
}
