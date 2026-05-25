using RouterNode.Domain.Routing;
using RouterNode.Tests.Shared;

namespace RouterNode.Tests.Domain.TestData;

public sealed class PriceRoutingCasesData : BaseDataGenerator
{
    public override IEnumerator<object[]> GetEnumerator()
    {
        yield return [999_999.99m, RouteChannel.LowPrice];
        yield return [1_000_000m, RouteChannel.HighPrice];
    }
}
