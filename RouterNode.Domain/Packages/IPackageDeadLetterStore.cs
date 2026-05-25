using RouterNode.Domain.Entities;

namespace RouterNode.Domain.Packages;

public interface IPackageDeadLetterStore
{
    Task MoveAsync(InboxPackage package, Exception reason, CancellationToken cancellationToken);
}