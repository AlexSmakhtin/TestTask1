using RouterNode.Domain.Entities;

namespace RouterNode.Domain.Routing;

public interface IPackageRoutingPolicy
{
    IReadOnlyList<RoutingDecision> GetRouteDecisions(PackagePassport passport);
}
