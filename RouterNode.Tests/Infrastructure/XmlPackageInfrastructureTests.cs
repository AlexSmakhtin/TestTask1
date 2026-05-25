using RouterNode.Domain.Entities;
using RouterNode.Domain.Routing;
using RouterNode.Infrastructure.Packages;
using Xunit;

namespace RouterNode.Tests.Infrastructure;

public class XmlPackageInfrastructureTests
{
    [Fact]
    public async Task XmlPackagePassportReader_ReadsValidPassport()
    {
        // Arrange
        using var workspace = await TestTemporaryWorkspace.CreateAsync();
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
        var reader = new XmlPackagePassportReader(workspace.PathResolver);

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
        using var workspace = await TestTemporaryWorkspace.CreateAsync();
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
        var reader = new XmlPackagePassportReader(workspace.PathResolver);

        // Act-Assert
        await Assert.ThrowsAsync<System.Xml.Schema.XmlSchemaValidationException>(() =>
            reader.ReadAsync(new InboxPackage("package", packageDirectory), CancellationToken.None));
    }

    [Fact]
    public async Task XmlOutgoingPackageWriter_CopiesAttachmentAndWritesNewPassport()
    {
        // Arrange
        using var workspace = await TestTemporaryWorkspace.CreateAsync();
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
        await File.WriteAllTextAsync(workspace.PathResolver.GetSourceAttachmentPath(sourceInboxPackage, "data.txt"),
            "payload");
        var writer = new XmlOutgoingPackageWriter(workspace.PathResolver, new XmlOutgoingPackagePassportWriter());
        var decision = new RoutingDecision(new PackageItem("order-1", "data.txt", "Data", null, 1, 10m), "order-1");

        // Act
        var draft = await writer.PrepareAsync(sourceInboxPackage, decision, CancellationToken.None);
        await writer.PublishAsync(draft, CancellationToken.None);

        // Assert
        var targetDirectory = workspace.PathResolver.GetOutgoingPackageDirectory(decision);
        Assert.Equal("payload",
            await File.ReadAllTextAsync(workspace.PathResolver.GetTargetAttachmentPath(targetDirectory, "data.txt")));
        Assert.True(File.Exists(workspace.PathResolver.GetPassportPath(targetDirectory)));
    }

    [Fact]
    public async Task XmlOutgoingPackageWriter_WhenAttachmentIsMissing_DoesNotLeaveTargetPackage()
    {
        // Arrange
        using var workspace = await TestTemporaryWorkspace.CreateAsync();
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
        var writer = new XmlOutgoingPackageWriter(workspace.PathResolver, new XmlOutgoingPackagePassportWriter());
        var decision = new RoutingDecision(new PackageItem("order-1", "missing.txt", "Data", null, 1, 10m), "order-1");

        // Act-Assert
        await Assert
            .ThrowsAsync<FileNotFoundException>(() => writer
                .PrepareAsync(new InboxPackage("package", sourcePackage), decision, CancellationToken.None));
        Assert.False(Directory.Exists(workspace.PathResolver.GetOutgoingPackageDirectory(decision)));
    }
}