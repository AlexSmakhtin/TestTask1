namespace RouterNode.Application.Abstractions;

using Packages;

public interface IPackageInbox
{
    IReadOnlyList<InboxPackage> GetReadyPackages();
}
