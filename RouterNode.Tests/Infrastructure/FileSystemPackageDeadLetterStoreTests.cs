using Microsoft.Extensions.Options;
using RouterNode.Application.Packages;
using RouterNode.Infrastructure.Files;
using Xunit;

namespace RouterNode.Tests.Infrastructure;

public sealed class FileSystemPackageDeadLetterStoreTests
{
    [Fact]
    public async Task MoveAsync_MovesPackageToDeadLetterDirectoryAndWritesErrorDetails()
    {
        // Arrange
        var options = TestFileSystemPackageOptionsBuilder.Create();
        var paths = new PackageFileSystemPathsHelper(Options.Create(options));
        using var workspace = new TestTemporaryWorkspace(options);
        await workspace.InitializeAsync();
        var packageDirectory = await workspace
            .CreatePackage("""
                           <?xml version="1.0" encoding="utf-8"?>
                           <shiporder>
                             <item orderid="order-1">
                               <attachment>missing.txt</attachment>
                               <title>Data</title>
                               <quantity>1</quantity>
                               <price>10</price>
                             </item>
                           </shiporder>
                           """);
        var package = new InboxPackage("package-1", packageDirectory);
        var deadLetterStore = new FileSystemPackageDeadLetterStore(paths);

        // Act
        await deadLetterStore.MoveAsync(package, new InvalidDataException("Invalid package."), CancellationToken.None);

        // Assert
        Assert.False(Directory.Exists(packageDirectory));
        var deadLetterPackage = Assert.Single(Directory.GetDirectories(options.DeadLetterPath));
        Assert.True(File.Exists(Path.Combine(deadLetterPackage, options.PassportFileName)));
        Assert.Contains("Invalid package.", await File.ReadAllTextAsync(Path.Combine(deadLetterPackage, "error.txt")));
    }
}