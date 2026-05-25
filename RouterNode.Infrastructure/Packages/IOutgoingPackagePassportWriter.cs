using RouterNode.Domain.Entities;

namespace RouterNode.Infrastructure.Packages;

public interface IOutgoingPackagePassportWriter
{
    Task WriteAsync(Stream stream, PackageItem item, CancellationToken cancellationToken);
}