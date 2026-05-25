using RouterNode.Domain.Entities;
using RouterNode.Domain.Files;
using RouterNode.Domain.Packages;
using RouterNode.Domain.Routing;
using RouterNode.Infrastructure.Files;

namespace RouterNode.Infrastructure.Packages;

public class XmlOutgoingPackageWriter(IPackageFilePathResolver pathResolver,
    IOutgoingPackagePassportWriter passportWriter)
    : IOutgoingPackageWriter
{
    public bool IsAlreadyWritten(RoutingDecision decision)
    {
        var targetDirectory = pathResolver.GetOutgoingPackageDirectory(decision);

        return Directory.Exists(targetDirectory) && pathResolver.HasPassport(targetDirectory);
    }

    public void EnsureCanWrite(InboxPackage sourcePackage, IReadOnlyCollection<RoutingDecision> decisions)
    {
        foreach (var decision in decisions.Where(decision => !IsAlreadyWritten(decision)))
        {
            EnsureAttachmentExists(sourcePackage, decision.Item);
        }
    }

    public async Task<OutgoingPackageDraft> PrepareAsync(InboxPackage sourcePackage, RoutingDecision decision,
        CancellationToken cancellationToken)
    {
        var channelDirectory = pathResolver.GetChannelDirectory(decision.Item.RouteChannel);
        var targetDirectory = pathResolver.GetOutgoingPackageDirectory(decision);
        var temporaryDirectory = pathResolver.GetTemporaryOutgoingPackageDirectory(decision);

        Directory.CreateDirectory(channelDirectory);
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            await CopyAttachmentAsync(sourcePackage, temporaryDirectory, decision.Item, cancellationToken);
            await WritePassportAsync(temporaryDirectory, decision.Item, cancellationToken);

            return new OutgoingPackageDraft(decision, temporaryDirectory, targetDirectory);
        }
        catch
        {
            RemoveDirectoryIfExists(temporaryDirectory);

            throw;
        }
    }

    public Task PublishAsync(OutgoingPackageDraft draft, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RemoveDirectoryIfExists(draft.TargetDirectory);
        Directory.Move(draft.TemporaryDirectory, draft.TargetDirectory);

        return Task.CompletedTask;
    }

    public void RemovePublished(OutgoingPackageDraft draft) => RemoveDirectoryIfExists(draft.TargetDirectory);

    public void RemoveTemporary(OutgoingPackageDraft draft) => RemoveDirectoryIfExists(draft.TemporaryDirectory);

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
        if (string.Equals(item.Attachment, pathResolver.PassportFileName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var attachmentFileName = Path.GetFileName(item.Attachment);
        var sourcePath = pathResolver.GetSourceAttachmentPath(sourcePackage, attachmentFileName);
        EnsureAttachmentExists(sourcePackage, item);

        var targetPath = pathResolver.GetTargetAttachmentPath(targetDirectory, attachmentFileName);

        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: FileSystemDefaults.StreamBufferSize, useAsync: true);
        await using var target = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: FileSystemDefaults.StreamBufferSize, useAsync: true);
        await source.CopyToAsync(target, cancellationToken);
    }

    private void EnsureAttachmentExists(InboxPackage sourcePackage, PackageItem item)
    {
        if (string.Equals(item.Attachment, pathResolver.PassportFileName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var attachmentFileName = Path.GetFileName(item.Attachment);
        var sourcePath = pathResolver.GetSourceAttachmentPath(sourcePackage, attachmentFileName);
        if (File.Exists(sourcePath))
        {
            return;
        }

        throw new FileNotFoundException(
            $"Attachment '{attachmentFileName}' was not found in package '{sourcePackage.FullPath}'.",
            sourcePath);
    }

    private async Task WritePassportAsync(string targetDirectory, PackageItem item,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(pathResolver.GetPassportPath(targetDirectory), FileMode.Create,
            FileAccess.Write, FileShare.None, bufferSize: FileSystemDefaults.StreamBufferSize, useAsync: true);
        await passportWriter.WriteAsync(stream, item, cancellationToken);
    }
}
