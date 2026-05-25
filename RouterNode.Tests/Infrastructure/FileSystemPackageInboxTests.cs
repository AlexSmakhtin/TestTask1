using RouterNode.Infrastructure.Files;
using Xunit;

namespace RouterNode.Tests.Infrastructure;

public sealed class FileSystemPackageInboxTests
{
    [Fact]
    public async Task GetReadyPackagesAsync_MovesInboxPackageToProcessingAndReturnsItFromProcessing()
    {
        // Arrange
        using var workspace = await TestTemporaryWorkspace.CreateAsync();
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
        var inbox = new FileSystemPackageInbox(workspace.PathResolver);

        // Act
        var firstRead = inbox.GetReadyPackages();
        var secondRead = inbox.GetReadyPackages();
        var processingPackageDirectory = workspace.PathResolver
            .GetProcessingPackageDirectory(Path.GetFileName(inboxPackageDirectory));

        // Assert
        Assert.False(Directory.Exists(inboxPackageDirectory));
        Assert.True(Directory.Exists(processingPackageDirectory));
        Assert.Equal(processingPackageDirectory, firstRead[0].FullPath);
        Assert.Equal(processingPackageDirectory, secondRead[0].FullPath);
    }

    [Fact]
    public async Task GetReadyPackagesAsync_WhenPassportIsLocked_DoesNotMovePackageToProcessing()
    {
        // Arrange
        using var workspace = await TestTemporaryWorkspace.CreateAsync();
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
        var passportPath = workspace.PathResolver.GetPassportPath(inboxPackageDirectory);
        await using var _ = new FileStream(passportPath, FileMode.Open, FileAccess.Read, FileShare.None);
        var inbox = new FileSystemPackageInbox(workspace.PathResolver);

        // Act
        var readyPackages = inbox.GetReadyPackages();

        // Assert
        Assert.Empty(readyPackages);
        Assert.True(Directory.Exists(inboxPackageDirectory));
        Assert.False(Directory.Exists(workspace.PathResolver
            .GetProcessingPackageDirectory(Path.GetFileName(inboxPackageDirectory))));
    }
}