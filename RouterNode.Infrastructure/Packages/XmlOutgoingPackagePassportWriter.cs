using System.Xml;
using System.Xml.Serialization;
using RouterNode.Domain.Entities;
using RouterNode.Infrastructure.Packages.XmlModels;

namespace RouterNode.Infrastructure.Packages;

public sealed class XmlOutgoingPackagePassportWriter : IOutgoingPackagePassportWriter
{
    private XmlSerializer XmlSerializer { get; } = new(typeof(ShipOrderXml));

    private XmlWriterSettings XmlWriterSettings { get; } = new()
    {
        Async = true,
        Indent = true,
        Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
    };

    public async Task WriteAsync(Stream stream, PackageItem item, CancellationToken cancellationToken)
    {
        await using var writer = XmlWriter.Create(stream, XmlWriterSettings);

        XmlSerializer.Serialize(writer, ShipOrderXml.FromDomain(item));
        await writer.FlushAsync();
        cancellationToken.ThrowIfCancellationRequested();
    }
}