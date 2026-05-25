namespace RouterNode.Domain.Entities;

public record PackagePassport
{
    public IReadOnlyList<PackageItem> Items { get; }

    public PackagePassport(IReadOnlyList<PackageItem> items)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("Package passport must contain at least one item.", nameof(items));
        }

        Items = items;
    }
}