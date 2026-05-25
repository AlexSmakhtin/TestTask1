namespace RouterNode.Application.Abstractions;

using Packages;

public interface IPackageDeadLetterStore
{
    Task MoveAsync(InboxPackage package, Exception reason, CancellationToken cancellationToken);
}