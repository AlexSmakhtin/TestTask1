using RouterNode.Domain.Packages;

namespace RouterNode.Domain.Routing;

public interface IPackageRoutingPolicy
{
    IReadOnlyList<RoutingDecision> Route(PackagePassport passport);
}
