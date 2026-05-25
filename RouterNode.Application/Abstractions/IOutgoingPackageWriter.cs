using RouterNode.Domain.Routing;
using RouterNode.Application.Packages;

namespace RouterNode.Application.Abstractions;

public interface IOutgoingPackageWriter
{
    bool IsAlreadyWritten(RoutingDecision decision);

    void EnsureCanWrite(InboxPackage sourcePackage, IReadOnlyCollection<RoutingDecision> decisions);

    Task WriteAsync(InboxPackage sourcePackage, RoutingDecision decision, CancellationToken cancellationToken);
}
