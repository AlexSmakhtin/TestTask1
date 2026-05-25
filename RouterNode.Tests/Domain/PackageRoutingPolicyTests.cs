using RouterNode.Domain.Packages;
using RouterNode.Domain.Routing;
using RouterNode.Tests.Domain.TestData;
using Xunit;

namespace RouterNode.Tests.Domain;

public sealed class PackageRoutingPolicyTests
{
    [Theory]
    [ClassData(typeof(PriceRoutingCasesData))]
    public void Route_SelectsExpectedChannelByPrice(decimal price, RouteChannel expectedChannel)
    {
        // Arrange
        var policy = new PackageRoutingPolicy(new SafePackageFolderNamePolicy());
        var passport = new PackagePassport([new PackageItem("order-1", "a.txt", "Item", null, 1, price)]);

        // Act
        var decision = policy.Route(passport)[0];

        // Assert
        Assert.Equal(expectedChannel, decision.Item.RouteChannel);
    }

    [Theory]
    [ClassData(typeof(UnsafeOrderIdsData))]
    public void CreateFolderName_DoesNotReturnUnsafePathSegments(string orderId)
    {
        // Arrange
        var policy = new SafePackageFolderNamePolicy();

        // Act
        var folderName = policy.CreateFolderName(orderId);

        // Assert
        Assert.DoesNotContain("\\", folderName);
        Assert.DoesNotContain(":", folderName);
        Assert.NotEqual(".", folderName);
        Assert.NotEmpty(folderName);
    }
}
