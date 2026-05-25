using RouterNode.Application.Abstractions;
using RouterNode.Application.Packages;

namespace RouterNode.Infrastructure.Files;

public sealed class FileSystemPackageDeadLetterStore(IPackageFileSystemPaths pathsHelper)
    : IPackageDeadLetterStore
{
    private const string ErrorFileName = "error.txt";

    public Task MoveAsync(InboxPackage package, Exception reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(package.FullPath))
        {
            return Task.CompletedTask;
        }

        Directory.CreateDirectory(pathsHelper.DeadLetterPath);

        var deadLetterDirectory = pathsHelper.GetDeadLetterPackageDirectory(package, DateTimeOffset.UtcNow);
        Directory.Move(package.FullPath, deadLetterDirectory);

        return File
            .WriteAllTextAsync(Path.Combine(deadLetterDirectory, ErrorFileName), reason.ToString(), cancellationToken);
    }
}