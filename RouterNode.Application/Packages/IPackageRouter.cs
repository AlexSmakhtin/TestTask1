namespace RouterNode.Application.Packages;

public interface IPackageRouter
{
    Task<PackageProcessingResult> ProcessReadyPackagesAsync(CancellationToken cancellationToken);
}