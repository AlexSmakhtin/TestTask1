using RouterNode.Tests.Shared;

namespace RouterNode.Tests.Domain.TestData;

public sealed class UnsafeOrderIdsData : BaseDataGenerator
{
    public override IEnumerator<object[]> GetEnumerator()
    {
        yield return ["C:\\lol"];
        yield return ["."];
        yield return ["C:\\kek\\"];
    }
}
