using System.Xml;
using System.Xml.Serialization;
using RouterNode.Application.Abstractions;
using RouterNode.Application.Packages;
using RouterNode.Domain.Packages;
using RouterNode.Domain.Routing;
using RouterNode.Infrastructure.Files;
using RouterNode.Infrastructure.Packages.XmlModels;

namespace RouterNode.Infrastructure.Packages;

public sealed class XmlOutgoingPackageWriter(IPackageFileSystemPaths pathsHelper)
    : IOutgoingPackageWriter
{
    private XmlSerializer XmlSerializer { get; } = new(typeof(ShipOrderXml));

    private XmlWriterSettings XmlWriterSettings { get; } = new()
    {
        Async = true,
        Indent = true,
        Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
    };

    public bool Exists(RoutingDecision decision)
    {
        var targetDirectory = pathsHelper.GetOutgoingPackageDirectory(decision);

        return Directory.Exists(targetDirectory) && pathsHelper.HasPassport(targetDirectory);
    }

    public async Task WriteAsync(InboxPackage sourcePackage, RoutingDecision decision,
        CancellationToken cancellationToken)
    {
        var channelDirectory = pathsHelper.GetChannelDirectory(decision.Item.RouteChannel);
        var targetDirectory = pathsHelper.GetOutgoingPackageDirectory(decision);
        var temporaryDirectory = pathsHelper.GetTemporaryOutgoingPackageDirectory(decision);

        Directory.CreateDirectory(channelDirectory);
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            await CopyAttachmentAsync(sourcePackage, temporaryDirectory, decision.Item, cancellationToken);
            await WritePassportAsync(temporaryDirectory, decision.Item);

            RemoveDirectoryIfExists(targetDirectory);

            Directory.Move(temporaryDirectory, targetDirectory);
        }
        catch
        {
            RemoveDirectoryIfExists(temporaryDirectory);

            throw;
        }
    }

    private static void RemoveDirectoryIfExists(string targetDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, recursive: true);
        }
    }

    private async Task CopyAttachmentAsync(InboxPackage sourcePackage, string targetDirectory, PackageItem item,
        CancellationToken cancellationToken)
    {
        if (string.Equals(item.Attachment, pathsHelper.PassportFileName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var attachmentFileName = Path.GetFileName(item.Attachment);
        var sourcePath = pathsHelper.GetSourceAttachmentPath(sourcePackage, attachmentFileName);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                $"Attachment '{attachmentFileName}' was not found in package '{sourcePackage.FullPath}'.",
                sourcePath);
        }

        var targetPath = pathsHelper.GetTargetAttachmentPath(targetDirectory, attachmentFileName);
        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: FileSystemDefaults.StreamBufferSize, useAsync: true);
        await using var target = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: FileSystemDefaults.StreamBufferSize, useAsync: true);
        await source.CopyToAsync(target, cancellationToken);
    }

    private async Task WritePassportAsync(string targetDirectory, PackageItem item)
    {
        await using var stream = new FileStream(pathsHelper.GetPassportPath(targetDirectory), FileMode.Create,
            FileAccess.Write, FileShare.None, bufferSize: FileSystemDefaults.StreamBufferSize, useAsync: true);
        await using var writer = XmlWriter.Create(stream, XmlWriterSettings);

        XmlSerializer.Serialize(writer, ShipOrderXml.FromDomain(item));
        await writer.FlushAsync();
    }
}