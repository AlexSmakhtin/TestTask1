using RouterNode.Domain.Entities;

namespace RouterNode.Domain.Routing;

public record RoutingDecision(PackageItem Item, string PackageFolderName);
