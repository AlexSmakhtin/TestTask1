using RouterNode.Domain.Routing;
using RouterNode.Application.Packages;

namespace RouterNode.Application.Abstractions;

public interface IOutgoingPackageWriter
{
    bool Exists(RoutingDecision decision);

    Task WriteAsync(InboxPackage sourcePackage, RoutingDecision decision, CancellationToken cancellationToken);
}
