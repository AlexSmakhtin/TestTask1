using RouterNode.Domain.Entities;

namespace RouterNode.Domain.Packages;

public interface IPackageInbox
{
    IReadOnlyList<InboxPackage> GetReadyPackages();
}
