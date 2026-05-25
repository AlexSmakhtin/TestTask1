using RouterNode.Domain.Entities;
using RouterNode.Domain.Routing;

namespace RouterNode.Domain.Packages;

public interface IOutgoingPackageWriter
{
    bool IsAlreadyWritten(RoutingDecision decision);

    void EnsureCanWrite(InboxPackage sourcePackage, IReadOnlyCollection<RoutingDecision> decisions);

    Task<OutgoingPackageDraft> PrepareAsync(InboxPackage sourcePackage, RoutingDecision decision,
        CancellationToken cancellationToken);

    Task PublishAsync(OutgoingPackageDraft draft, CancellationToken cancellationToken);

    void RemovePublished(OutgoingPackageDraft draft);

    void RemoveTemporary(OutgoingPackageDraft draft);
}
