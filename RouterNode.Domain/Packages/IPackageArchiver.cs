using RouterNode.Domain.Entities;

namespace RouterNode.Domain.Packages;

public interface IPackageArchiver
{
    Task ArchiveAsync(InboxPackage package, CancellationToken cancellationToken);
}