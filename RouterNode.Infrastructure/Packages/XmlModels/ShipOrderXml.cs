using System.Xml.Serialization;
using RouterNode.Domain.Entities;

namespace RouterNode.Infrastructure.Packages.XmlModels;

[XmlRoot("shiporder")]
public class ShipOrderXml
{
    [XmlElement("item")]
    public List<ShipOrderItemXml> Items { get; init; } = [];

    public PackagePassport ToDomain() => new(Items.Select(item => item.ToDomain()).ToArray());

    public static ShipOrderXml FromDomain(PackageItem item) => new() { Items = [ShipOrderItemXml.FromDomain(item)] };
}