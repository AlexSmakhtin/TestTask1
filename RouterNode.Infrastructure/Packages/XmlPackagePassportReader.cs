using System.Xml;
using System.Xml.Serialization;
using RouterNode.Application.Abstractions;
using RouterNode.Application.Packages;
using RouterNode.Domain.Packages;
using RouterNode.Infrastructure.Files;
using RouterNode.Infrastructure.Packages.XmlModels;

namespace RouterNode.Infrastructure.Packages;

public class XmlPackagePassportReader : IPackagePassportReader
{
    private IPackageFileSystemPaths PathsHelper { get; }

    private XmlReaderSettings XmlReaderSettings { get; }

    public XmlPackagePassportReader(IPackageFileSystemPaths pathsHelper)
    {
        PathsHelper = pathsHelper;
        XmlReaderSettings = new XmlReaderSettings
        {
            Async = true,
            ValidationType = ValidationType.Schema
        };

        XmlReaderSettings.Schemas.Add(targetNamespace: null, PathsHelper.SchemaPath);
    }

    private static readonly XmlSerializer Serializer = new(typeof(ShipOrderXml));

    public Task<PackagePassport> ReadAsync(InboxPackage package, CancellationToken cancellationToken)
    {
        var passportPath = PathsHelper.GetPassportPath(package.FullPath);

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
        if (!File.Exists(PathsHelper.SchemaPath))
        {
            throw new FileNotFoundException("Package XML schema was not found.", PathsHelper.SchemaPath);
        }

        using var textReader = new StringReader(passportXml);
        using var xmlReader = XmlReader.Create(textReader, XmlReaderSettings);

        while (await xmlReader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}