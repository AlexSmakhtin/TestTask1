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
            .Where(IsReadyPackage)
            .OrderBy(Directory.GetCreationTimeUtc)
            .Select(x => new InboxPackage(Path.GetFileName(x), x))
            .ToArray();
    }

    private void MoveReadyPackagesToProcessing()
    {
        foreach (var packageDirectory in Directory
                     .EnumerateDirectories(pathsHelper.InboxPath)
                     .Where(IsReadyPackage)
                     .OrderBy(Directory.GetCreationTimeUtc))
        {
            var packageName = Path.GetFileName(packageDirectory);
            var processingDirectory = pathsHelper.GetProcessingPackageDirectory(packageName);

            if (Directory.Exists(processingDirectory))
            {
                continue;
            }

            try
            {
                Directory.Move(packageDirectory, processingDirectory);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private bool IsReadyPackage(string packageDirectory)
    {
        var passportPath = pathsHelper.GetPassportPath(packageDirectory);
        if (!File.Exists(passportPath))
        {
            return false;
        }

        try
        {
            using var _ = new FileStream(passportPath, FileMode.Open, FileAccess.Read, FileShare.None);

            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}