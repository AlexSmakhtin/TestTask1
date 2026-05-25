using Microsoft.Extensions.Options;
using RouterNode.Application.Packages;
using RouterNode.Domain.Packages;
using RouterNode.Domain.Routing;
using RouterNode.Infrastructure.Files;
using RouterNode.Infrastructure.Packages;
using Xunit;

namespace RouterNode.Tests.Infrastructure;

public class XmlPackageInfrastructureTests
{
    [Fact]
    public async Task XmlPackagePassportReader_ReadsValidPassport()
    {
        // Arrange
        var options = TestFileSystemPackageOptionsBuilder.Create();
        IPackageFileSystemPaths paths = new PackageFileSystemPathsHelper(Options.Create(options));
        using var workspace = new TestTemporaryWorkspace(options);
        await workspace.InitializeAsync();
        var packageDirectory = await workspace
            .CreatePackage("""
                           <?xml version="1.0" encoding="utf-8"?>
                           <shiporder>
                             <item orderid="b5fcfc1ef986c1a3">
                               <attachment>photo.png</attachment>
                               <title>Macbook</title>
                               <note>Gray</note>
                               <quantity>8016</quantity>
                               <price>1937719.7650942</price>
                             </item>
                           </shiporder>
                           """);
        var reader = new XmlPackagePassportReader(paths);

        // Act
        var passport = await reader.ReadAsync(new InboxPackage("package", packageDirectory), CancellationToken.None);

        // Assert
        var item = passport.Items.Single();
        Assert.Equal("b5fcfc1ef986c1a3", item.OrderId);
        Assert.Equal("photo.png", item.Attachment);
        Assert.Equal(1937719.7650942m, item.Price);
    }

    [Fact]
    public async Task XmlPackagePassportReader_RejectsPassportThatDoesNotMatchSchema()
    {
        // Arrange
        var options = TestFileSystemPackageOptionsBuilder.Create();
        IPackageFileSystemPaths paths = new PackageFileSystemPathsHelper(Options.Create(options));
        using var workspace = new TestTemporaryWorkspace(options);
        await workspace.InitializeAsync();
        var packageDirectory = await workspace
            .CreatePackage("""
                           <?xml version="1.0" encoding="utf-8"?>
                           <shiporder>
                             <item orderid="C:\kek\">
                               <file>kek.kek</file>
                               <title>LMAO v2</title>
                               <quantity>9542</quantity>
                               <price>1411840.2349058</price>
                             </item>
                           </shiporder>
                           """);
        var reader = new XmlPackagePassportReader(paths);

        // Act-Assert
        await Assert.ThrowsAsync<System.Xml.Schema.XmlSchemaValidationException>(() =>
            reader.ReadAsync(new InboxPackage("package", packageDirectory), CancellationToken.None));
    }

    [Fact]
    public async Task XmlOutgoingPackageWriter_CopiesAttachmentAndWritesNewPassport()
    {
        // Arrange
        var options = TestFileSystemPackageOptionsBuilder.Create();
        var paths = new PackageFileSystemPathsHelper(Options.Create(options));
        using var workspace = new TestTemporaryWorkspace(options);
        await workspace.InitializeAsync();
        var sourcePackage = await workspace
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
        var sourceInboxPackage = new InboxPackage("package", sourcePackage);
        await File.WriteAllTextAsync(paths.GetSourceAttachmentPath(sourceInboxPackage, "data.txt"), "payload");
        var writer = new XmlOutgoingPackageWriter(paths);
        var decision = new RoutingDecision(new PackageItem("order-1", "data.txt", "Data", null, 1, 10m), "order-1");

        // Act
        await writer.WriteAsync(sourceInboxPackage, decision, CancellationToken.None);

        // Assert
        var targetDirectory = paths.GetOutgoingPackageDirectory(decision);
        Assert.Equal("payload",
            await File.ReadAllTextAsync(paths.GetTargetAttachmentPath(targetDirectory, "data.txt")));
        Assert.True(File.Exists(paths.GetPassportPath(targetDirectory)));
    }

    [Fact]
    public async Task XmlOutgoingPackageWriter_WhenAttachmentIsMissing_DoesNotLeaveTargetPackage()
    {
        // Arrange
        var options = TestFileSystemPackageOptionsBuilder.Create();
        var paths = new PackageFileSystemPathsHelper(Options.Create(options));
        using var workspace = new TestTemporaryWorkspace(options);
        await workspace.InitializeAsync();
        var sourcePackage = await workspace
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
        var writer = new XmlOutgoingPackageWriter(paths);
        var decision = new RoutingDecision(new PackageItem("order-1", "missing.txt", "Data", null, 1, 10m), "order-1");

        // Act-Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            writer.WriteAsync(new InboxPackage("package", sourcePackage), decision, CancellationToken.None));
        Assert.False(Directory.Exists(paths.GetOutgoingPackageDirectory(decision)));
    }
}