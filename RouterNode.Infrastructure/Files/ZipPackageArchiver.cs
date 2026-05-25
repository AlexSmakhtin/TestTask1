using System.IO.Compression;
using RouterNode.Domain.Entities;
using RouterNode.Domain.Files;
using RouterNode.Domain.Packages;

namespace RouterNode.Infrastructure.Files;

public class ZipPackageArchiver(IPackageFilePathResolver pathResolver)
    : IPackageArchiver
{
    public async Task ArchiveAsync(InboxPackage package, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(pathResolver.ArchivePath);

        var archivePath = pathResolver.GetArchivePath(package, DateTimeOffset.UtcNow);
        var temporaryArchivePath = $"{archivePath}.tmp";

        try
        {
            await ZipFile
                .CreateFromDirectoryAsync(package.FullPath, temporaryArchivePath, CompressionLevel.Optimal,
                    includeBaseDirectory: true, cancellationToken: cancellationToken);
            File.Move(temporaryArchivePath, archivePath);

            Directory.Delete(package.FullPath, recursive: true);
        }
        catch
        {
            RemoveFileIfExists(temporaryArchivePath);
            RemoveFileIfExists(archivePath);

            throw;
        }
    }

    private static void RemoveFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
