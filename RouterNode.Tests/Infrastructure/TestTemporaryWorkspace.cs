using RouterNode.Infrastructure.Files;

namespace RouterNode.Tests.Infrastructure;

public class TestTemporaryWorkspace : IDisposable
{
    private string Path { get; }

    private FileSystemPackageOptions Options { get; }

    private IPackageFileSystemPaths PathsHelper { get; }

    public TestTemporaryWorkspace(FileSystemPackageOptions options)
    {
        Options = options;
        PathsHelper = new PackageFileSystemPathsHelper(Microsoft.Extensions.Options.Options.Create(options));
        Path = PathsHelper.WorkspacePath;

        Directory.CreateDirectory(PathsHelper.InboxPath);
        Directory.CreateDirectory(PathsHelper.OutboxPath);
        Directory.CreateDirectory(PathsHelper.ProcessingPath);
        Directory.CreateDirectory(PathsHelper.ArchivePath);
    }

    public async Task InitializeAsync()
    {
        await File.WriteAllTextAsync(PathsHelper.SchemaPath, TestXmlSchemas.PackageSchema);
    }

    public async Task<string> CreatePackage(string passport)
    {
        var packageDirectory = PathsHelper.GetInboxPackageDirectory(Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packageDirectory);
        await File.WriteAllTextAsync(PathsHelper.GetPassportPath(packageDirectory), passport);

        return packageDirectory;
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
