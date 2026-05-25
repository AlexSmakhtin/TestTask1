using Microsoft.Extensions.Options;
using RouterNode.Application.Packages;
using RouterNode.Domain.Routing;

namespace RouterNode.Infrastructure.Files;

public class PackageFileSystemPathsHelper(IOptions<FileSystemPackageOptions> options)
    : IPackageFileSystemPaths
{
    private FileSystemPackageOptions Options { get; } = options.Value;

    private const string LowPriceChannelFolderName = "lowprice";

    private const string HighPriceChannelFolderName = "highprice";

    public string InboxPath => Options.InboxPath;

    public string ArchivePath => Options.ArchivePath;

    public string SchemaPath => Options.SchemaPath;

    public string OutboxPath => Options.OutboxPath;

    public string ProcessingPath => Options.ProcessingPath;

    public string PassportFileName => Options.PassportFileName;

    public string WorkspacePath =>
        Directory.GetParent(InboxPath)?.FullName
        ?? throw new InvalidOperationException("Inbox path must have a parent directory.");

    public string GetInboxPackageDirectory(string packageFolderName) => Path.Combine(InboxPath, packageFolderName);

    public string GetProcessingPackageDirectory(string packageFolderName)
        => Path.Combine(ProcessingPath, packageFolderName);

    public string GetPassportPath(string packageDirectory) => Path.Combine(packageDirectory, PassportFileName);

    public bool HasPassport(string packageDirectory) => File.Exists(GetPassportPath(packageDirectory));

    public string GetChannelDirectory(RouteChannel channel)
        => Path.Combine(Options.OutboxPath,
            channel == RouteChannel.HighPrice ? HighPriceChannelFolderName : LowPriceChannelFolderName);

    public string GetOutgoingPackageDirectory(RoutingDecision decision)
        => Path.Combine(GetChannelDirectory(decision.Item.RouteChannel), decision.PackageFolderName);

    public string GetTemporaryOutgoingPackageDirectory(RoutingDecision decision)
        => Path.Combine(GetChannelDirectory(decision.Item.RouteChannel),
            $"{decision.PackageFolderName}.tmp-{Guid.CreateVersion7():N}");

    public string GetSourceAttachmentPath(InboxPackage sourcePackage, string attachmentFileName)
        => Path.Combine(sourcePackage.FullPath, attachmentFileName);

    public string GetTargetAttachmentPath(string targetDirectory, string attachmentFileName)
        => Path.Combine(targetDirectory, attachmentFileName);

    public string GetArchivePath(InboxPackage package, DateTimeOffset archivedAt)
        => Path.Combine(ArchivePath, $"{package.Name}-{archivedAt:yyyyMMddHHmmssfff}.zip");
}