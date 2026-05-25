using RouterNode.Domain.Entities;

namespace RouterNode.Domain.Routing;

public interface IPackageRouter
{
    Task<PackageProcessingResult> ProcessReadyPackagesAsync(CancellationToken cancellationToken);
}