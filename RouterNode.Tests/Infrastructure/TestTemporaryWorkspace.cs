using RouterNode.Domain.Files;
using RouterNode.Infrastructure.Files;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace RouterNode.Tests.Infrastructure;

public sealed class TestTemporaryWorkspace : IDisposable
{
    private string WorkspacePath { get; }

    public FileSystemPackageOptions Options { get; }

    public IPackageFilePathResolver PathResolver { get; }

    private TestTemporaryWorkspace(FileSystemPackageOptions options)
    {
        Options = options;
        PathResolver = new PackageFilePathResolver(OptionsFactory.Create(options));
        WorkspacePath = Directory.GetParent(Options.InboxPath)?.FullName
                        ?? throw new InvalidOperationException("Inbox path must have a parent directory.");

        Directory.CreateDirectory(Options.InboxPath);
        Directory.CreateDirectory(Options.OutboxPath);
        Directory.CreateDirectory(Options.ProcessingPath);
        Directory.CreateDirectory(Options.ArchivePath);
        Directory.CreateDirectory(Options.DeadLetterPath);
    }

    public static async Task<TestTemporaryWorkspace> CreateAsync()
    {
        var workspace = new TestTemporaryWorkspace(TestFileSystemPackageOptionsBuilder.Create());
        await File.WriteAllTextAsync(workspace.Options.SchemaPath, TestXmlSchemas.PackageSchema);

        return workspace;
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