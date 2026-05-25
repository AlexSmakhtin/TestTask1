using RouterNode.Domain.Packages;
using RouterNode.Application.Packages;

namespace RouterNode.Application.Abstractions;

public interface IPackagePassportReader
{
    Task<PackagePassport> ReadAsync(InboxPackage package, CancellationToken cancellationToken);
}
