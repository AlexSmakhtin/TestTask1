using RouterNode.Domain.Entities;

namespace RouterNode.Domain.Packages;

public interface IPackagePassportReader
{
    Task<PackagePassport> ReadAsync(InboxPackage package, CancellationToken cancellationToken);
}
