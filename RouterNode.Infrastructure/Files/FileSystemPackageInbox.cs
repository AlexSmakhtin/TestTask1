using RouterNode.Application.Abstractions;
using RouterNode.Application.Packages;

namespace RouterNode.Infrastructure.Files;

public sealed class FileSystemPackageInbox(IPackageFileSystemPaths pathsHelper)
    : IPackageInbox
{
    public IReadOnlyList<InboxPackage> GetReadyPackages()
    {
        Directory.CreateDirectory(pathsHelper.InboxPath);
        Directory.CreateDirectory(pathsHelper.ProcessingPath);

        MoveReadyPackagesToProcessing();

        return Directory
            .EnumerateDirectories(pathsHelper.ProcessingPath)
            .Where(pathsHelper.HasPassport)
            .OrderBy(Directory.GetCreationTimeUtc)
            .Select(x => new InboxPackage(Path.GetFileName(x), x))
            .ToArray();
    }

    private void MoveReadyPackagesToProcessing()
    {
        foreach (var packageDirectory in Directory
                     .EnumerateDirectories(pathsHelper.InboxPath)
                     .Where(pathsHelper.HasPassport)
                     .OrderBy(Directory.GetCreationTimeUtc))
        {
            var packageName = Path.GetFileName(packageDirectory);
            var processingDirectory = pathsHelper.GetProcessingPackageDirectory(packageName);

            if (Directory.Exists(processingDirectory))
            {
                continue;
            }

            Directory.Move(packageDirectory, processingDirectory);
        }
    }
}