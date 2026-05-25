using RouterNode.Domain.Routing;

namespace RouterNode.Domain.Packages;

public sealed record OutgoingPackageDraft(RoutingDecision Decision, string TemporaryDirectory, string TargetDirectory);