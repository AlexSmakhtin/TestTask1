using RouterNode.Domain.Entities;
using RouterNode.Infrastructure.Files;
using Xunit;

namespace RouterNode.Tests.Infrastructure;

public sealed class FileSystemPackageDeadLetterStoreTests
{
    [Fact]
    public async Task MoveAsync_MovesPackageToDeadLetterDirectoryAndWritesErrorDetails()
    {
        // Arrange
        using var workspace = await TestTemporaryWorkspace.CreateAsync();
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
        var deadLetterStore = new FileSystemPackageDeadLetterStore(workspace.PathResolver);

        // Act
        await deadLetterStore.MoveAsync(package, new InvalidDataException("Invalid package."), CancellationToken.None);

        // Assert
        Assert.False(Directory.Exists(packageDirectory));
        var deadLetterPackage = Assert.Single(Directory.GetDirectories(workspace.Options.DeadLetterPath));
        Assert.True(File.Exists(Path.Combine(deadLetterPackage, workspace.Options.PassportFileName)));
        Assert.Contains("Invalid package.", await File.ReadAllTextAsync(Path.Combine(deadLetterPackage, "error.txt")));
    }
}