using RouterNode.Domain.Entities;
using RouterNode.Domain.Files;
using RouterNode.Domain.Packages;

namespace RouterNode.Infrastructure.Files;

public sealed class FileSystemPackageInbox(IPackageFilePathResolver pathResolver)
    : IPackageInbox
{
    public IReadOnlyList<InboxPackage> GetReadyPackages()
    {
        Directory.CreateDirectory(pathResolver.InboxPath);
        Directory.CreateDirectory(pathResolver.ProcessingPath);

        MoveReadyPackagesToProcessing();

        return Directory
            .EnumerateDirectories(pathResolver.ProcessingPath)
            .Where(IsReadyPackage)
            .OrderBy(Directory.GetCreationTimeUtc)
            .Select(x => new InboxPackage(Path.GetFileName(x), x))
            .ToArray();
    }

    private void MoveReadyPackagesToProcessing()
    {
        foreach (var packageDirectory in Directory
                     .EnumerateDirectories(pathResolver.InboxPath)
                     .Where(IsReadyPackage)
                     .OrderBy(Directory.GetCreationTimeUtc))
        {
            var packageName = Path.GetFileName(packageDirectory);
            var processingDirectory = pathResolver.GetProcessingPackageDirectory(packageName);

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
        var passportPath = pathResolver.GetPassportPath(packageDirectory);
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