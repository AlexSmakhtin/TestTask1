using RouterNode.Application.Packages;
using RouterNode.Domain.Routing;

namespace RouterNode.Infrastructure.Files;

public interface IPackageFileSystemPaths
{
    string InboxPath { get; }

    string ArchivePath { get; }

    string SchemaPath { get; }

    string ProcessingPath { get; }

    string PassportFileName { get; }

    string GetProcessingPackageDirectory(string packageFolderName);

    string GetPassportPath(string packageDirectory);

    bool HasPassport(string packageDirectory);

    string GetChannelDirectory(RouteChannel channel);

    string GetOutgoingPackageDirectory(RoutingDecision decision);

    string GetTemporaryOutgoingPackageDirectory(RoutingDecision decision);

    string GetSourceAttachmentPath(InboxPackage sourcePackage, string attachmentFileName);

    string GetTargetAttachmentPath(string targetDirectory, string attachmentFileName);

    string GetArchivePath(InboxPackage package, DateTimeOffset archivedAt);
}