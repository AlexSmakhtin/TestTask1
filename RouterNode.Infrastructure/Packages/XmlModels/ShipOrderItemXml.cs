using System.Globalization;
using System.Xml.Serialization;
using RouterNode.Domain.Entities;

namespace RouterNode.Infrastructure.Packages.XmlModels;

public class ShipOrderItemXml
{
    [XmlAttribute("orderid")]
    public string OrderId { get; init; } = null!;

    [XmlElement("attachment")]
    public string Attachment { get; init; } = null!;

    [XmlElement("title")]
    public string Title { get; init; } = null!;

    [XmlElement("note")]
    public string? Note { get; init; }

    [XmlElement("quantity")]
    public string Quantity { get; init; } = null!;

    [XmlElement("price")]
    public string Price { get; init; } = null!;

    public PackageItem ToDomain()
        => new(OrderId, Attachment, Title, Note, Quantity: int.Parse(Quantity, CultureInfo.InvariantCulture),
            Price: decimal.Parse(Price, CultureInfo.InvariantCulture));

    public static ShipOrderItemXml FromDomain(PackageItem item)
        => new()
        {
            OrderId = item.OrderId,
            Attachment = item.Attachment,
            Title = item.Title,
            Note = item.Note,
            Quantity = item.Quantity.ToString(CultureInfo.InvariantCulture),
            Price = item.Price.ToString(CultureInfo.InvariantCulture)
        };
}