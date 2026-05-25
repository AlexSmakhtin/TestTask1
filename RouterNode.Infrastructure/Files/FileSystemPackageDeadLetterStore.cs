using RouterNode.Domain.Entities;
using RouterNode.Domain.Files;
using RouterNode.Domain.Packages;

namespace RouterNode.Infrastructure.Files;

public sealed class FileSystemPackageDeadLetterStore(IPackageFilePathResolver pathResolver)
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

        Directory.CreateDirectory(pathResolver.DeadLetterPath);

        var deadLetterDirectory = pathResolver.GetDeadLetterPackageDirectory(package, DateTimeOffset.UtcNow);
        Directory.Move(package.FullPath, deadLetterDirectory);

        return File
            .WriteAllTextAsync(Path.Combine(deadLetterDirectory, ErrorFileName), reason.ToString(), cancellationToken);
    }
}