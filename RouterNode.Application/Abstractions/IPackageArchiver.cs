namespace RouterNode.Application.Abstractions;

using Packages;

public interface IPackageArchiver
{
    Task ArchiveAsync(InboxPackage package, CancellationToken cancellationToken);
}