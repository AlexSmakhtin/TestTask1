namespace RouterNode.Domain.Packages;

public record PackagePassport
{
    public IReadOnlyCollection<PackageItem> Items { get; }

    public PackagePassport(IReadOnlyCollection<PackageItem> items)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("Package passport must contain at least one item.", nameof(items));
        }

        Items = items;
    }
}