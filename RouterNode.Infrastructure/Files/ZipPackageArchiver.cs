using System.IO.Compression;
using RouterNode.Application.Abstractions;
using RouterNode.Application.Packages;

namespace RouterNode.Infrastructure.Files;

public class ZipPackageArchiver(IPackageFileSystemPaths pathsHelper)
    : IPackageArchiver
{
    public async Task ArchiveAsync(InboxPackage package, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(pathsHelper.ArchivePath);

        var archivePath = pathsHelper.GetArchivePath(package, DateTimeOffset.UtcNow);

        await ZipFile
            .CreateFromDirectoryAsync(package.FullPath, archivePath, CompressionLevel.Optimal,
                includeBaseDirectory: true, cancellationToken: cancellationToken);

        Directory.Delete(package.FullPath, recursive: true);
    }
}
