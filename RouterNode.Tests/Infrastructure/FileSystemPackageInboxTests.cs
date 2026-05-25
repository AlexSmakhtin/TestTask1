using Microsoft.Extensions.Options;
using RouterNode.Infrastructure.Files;
using Xunit;

namespace RouterNode.Tests.Infrastructure;

public sealed class FileSystemPackageInboxTests
{
    [Fact]
    public async Task GetReadyPackagesAsync_MovesInboxPackageToProcessingAndReturnsItFromProcessing()
    {
        // Arrange
        var options = TestFileSystemPackageOptionsBuilder.Create();
        var paths = new PackageFileSystemPathsHelper(Options.Create(options));
        using var workspace = new TestTemporaryWorkspace(options);
        await workspace.InitializeAsync();
        var inboxPackageDirectory = await workspace
            .CreatePackage("""
                           <?xml version="1.0" encoding="utf-8"?>
                           <shiporder>
                             <item orderid="order-1">
                               <attachment>data.txt</attachment>
                               <title>Data</title>
                               <quantity>1</quantity>
                               <price>10</price>
                             </item>
                           </shiporder>
                           """);
        var inbox = new FileSystemPackageInbox(paths);

        // Act
        var firstRead = inbox.GetReadyPackages();
        var secondRead = inbox.GetReadyPackages();
        var processingPackageDirectory = paths.GetProcessingPackageDirectory(Path.GetFileName(inboxPackageDirectory));

        // Assert
        Assert.False(Directory.Exists(inboxPackageDirectory));
        Assert.True(Directory.Exists(processingPackageDirectory));
        Assert.Equal(processingPackageDirectory, firstRead[0].FullPath);
        Assert.Equal(processingPackageDirectory, secondRead[0].FullPath);
    }
}