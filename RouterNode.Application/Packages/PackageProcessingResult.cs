namespace RouterNode.Application.Packages;

public sealed record PackageProcessingResult(int PackagesProcessed, int ItemsRouted, int PackagesFailed);
