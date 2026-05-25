using RouterNode.Infrastructure.Files;

namespace RouterNode.Tests.Infrastructure;

public class TestTemporaryWorkspace : IDisposable
{
    private string WorkspacePath { get; }

    private FileSystemPackageOptions Options { get; }

    public TestTemporaryWorkspace(FileSystemPackageOptions options)
    {
        Options = options;
        WorkspacePath = Directory.GetParent(Options.InboxPath)?.FullName
                        ?? throw new InvalidOperationException("Inbox path must have a parent directory.");

        Directory.CreateDirectory(Options.InboxPath);
        Directory.CreateDirectory(Options.OutboxPath);
        Directory.CreateDirectory(Options.ProcessingPath);
        Directory.CreateDirectory(Options.ArchivePath);
    }

    public async Task InitializeAsync()
    {
        await File.WriteAllTextAsync(Options.SchemaPath, TestXmlSchemas.PackageSchema);
    }

    public async Task<string> CreatePackage(string passport)
    {
        var packageDirectory = Path.Combine(Options.InboxPath, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packageDirectory);
        await File.WriteAllTextAsync(Path.Combine(packageDirectory, Options.PassportFileName), passport);

        return packageDirectory;
    }

    public void Dispose()
    {
        if (Directory.Exists(WorkspacePath))
        {
            Directory.Delete(WorkspacePath, recursive: true);
        }
    }
}