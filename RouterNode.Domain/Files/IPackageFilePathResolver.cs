using RouterNode.Domain.Entities;
using RouterNode.Domain.Routing;

namespace RouterNode.Domain.Files;

public interface IPackageFilePathResolver
{
    string InboxPath { get; }

    string ArchivePath { get; }

    string DeadLetterPath { get; }

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

    string GetDeadLetterPackageDirectory(InboxPackage package, DateTimeOffset failedAt);
}
