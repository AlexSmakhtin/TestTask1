using RouterNode.Domain.Packages;

namespace RouterNode.Domain.Routing;

public class PackageRoutingPolicy(IPackageFolderNamePolicy folderNamePolicy)
    : IPackageRoutingPolicy
{
    public IReadOnlyList<RoutingDecision> Route(PackagePassport passport)
        => passport.Items
            .Select(item => new RoutingDecision(item, folderNamePolicy.CreateFolderName(item.OrderId)))
            .ToArray();
}