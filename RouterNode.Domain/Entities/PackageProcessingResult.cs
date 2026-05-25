namespace RouterNode.Domain.Entities;

public record PackageProcessingResult(int PackagesProcessed, int ItemsRouted, int PackagesFailed);