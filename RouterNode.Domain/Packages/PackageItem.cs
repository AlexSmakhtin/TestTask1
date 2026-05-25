using RouterNode.Domain.Routing;

namespace RouterNode.Domain.Packages;

public record PackageItem(string OrderId,
    string Attachment,
    string Title,
    string? Note,
    int Quantity,
    decimal Price)
{
    private const decimal HighPriceThreshold = 1_000_000m;

    private bool IsHighPrice => Price >= HighPriceThreshold;

    public RouteChannel RouteChannel => IsHighPrice ? RouteChannel.HighPrice : RouteChannel.LowPrice;
}
