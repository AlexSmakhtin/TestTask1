using System.Collections;

namespace RouterNode.Tests.Shared;

public abstract class BaseDataGenerator : IEnumerable<object[]>
{
    public abstract IEnumerator<object[]> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
