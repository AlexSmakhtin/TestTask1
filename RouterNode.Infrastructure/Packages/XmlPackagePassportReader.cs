using System.Xml;
using System.Xml.Serialization;
using RouterNode.Domain.Entities;
using RouterNode.Domain.Files;
using RouterNode.Domain.Packages;
using RouterNode.Infrastructure.Packages.XmlModels;

namespace RouterNode.Infrastructure.Packages;

public class XmlPackagePassportReader : IPackagePassportReader
{
    private IPackageFilePathResolver PathResolver { get; }

    private XmlReaderSettings XmlReaderSettings { get; }

    public XmlPackagePassportReader(IPackageFilePathResolver pathResolver)
    {
        PathResolver = pathResolver;
        XmlReaderSettings = new XmlReaderSettings
        {
            Async = true,
            ValidationType = ValidationType.Schema
        };

        XmlReaderSettings.Schemas.Add(targetNamespace: null, PathResolver.SchemaPath);
    }

    private static readonly XmlSerializer Serializer = new(typeof(ShipOrderXml));

    public Task<PackagePassport> ReadAsync(InboxPackage package, CancellationToken cancellationToken)
    {
        var passportPath = PathResolver.GetPassportPath(package.FullPath);

        return ReadPassportAsync(passportPath, cancellationToken);
    }

    private async Task<PackagePassport> ReadPassportAsync(string passportPath, CancellationToken cancellationToken)
    {
        var passportXml = await File.ReadAllTextAsync(passportPath, cancellationToken);

        await ValidateAsync(passportXml, cancellationToken);

        using var textReader = new StringReader(passportXml);
        var package = (ShipOrderXml)(Serializer.Deserialize(textReader)
                                     ?? throw new InvalidDataException("Package passport is empty."));

        return package.ToDomain();
    }

    private async Task ValidateAsync(string passportXml, CancellationToken cancellationToken)
    {
        if (!File.Exists(PathResolver.SchemaPath))
        {
            throw new FileNotFoundException("Package XML schema was not found.", PathResolver.SchemaPath);
        }

        using var textReader = new StringReader(passportXml);
        using var xmlReader = XmlReader.Create(textReader, XmlReaderSettings);

        while (await xmlReader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}