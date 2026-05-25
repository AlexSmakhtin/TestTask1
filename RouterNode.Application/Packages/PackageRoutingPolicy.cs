using RouterNode.Domain.Entities;
using RouterNode.Domain.Packages;
using RouterNode.Domain.Routing;

namespace RouterNode.Application.Packages;

public class PackageRoutingPolicy(IPackageFolderNamePolicy folderNamePolicy)
    : IPackageRoutingPolicy
{
    public IReadOnlyList<RoutingDecision> GetRouteDecisions(PackagePassport passport)
        => passport.Items
            .Select(item => new RoutingDecision(item, folderNamePolicy.CreateFolderName(item.OrderId)))
            .ToArray();
}