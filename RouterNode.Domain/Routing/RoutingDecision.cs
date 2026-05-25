using RouterNode.Domain.Packages;

namespace RouterNode.Domain.Routing;

public record RoutingDecision(PackageItem Item, string PackageFolderName);
