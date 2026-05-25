using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RouterNode.Application.Abstractions;
using RouterNode.Application.Packages;
using RouterNode.Domain.Packages;
using RouterNode.Domain.Routing;
using Xunit;

namespace RouterNode.Tests.Application;

public sealed class PackageRouterTests
{
    private readonly Mock<IPackageInbox> inbox;
    private readonly Mock<IPackagePassportReader> reader;
    private readonly Mock<IOutgoingPackageWriter> writer;
    private readonly Mock<IPackageArchiver> archiver;
    private readonly Mock<IItemTransferNotifier> notifier;
    private readonly IPackageRoutingPolicy routingPolicy;
    private readonly PackageRouter router;

    public PackageRouterTests()
    {
        inbox = new Mock<IPackageInbox>();
        reader = new Mock<IPackagePassportReader>();
        writer = new Mock<IOutgoingPackageWriter>();
        archiver = new Mock<IPackageArchiver>();
        notifier = new Mock<IItemTransferNotifier>();
        routingPolicy = new PackageRoutingPolicy(new SafePackageFolderNamePolicy());
        router = new PackageRouter(
            inbox.Object,
            reader.Object,
            writer.Object,
            archiver.Object,
            notifier.Object,
            routingPolicy,
            NullLogger<PackageRouter>.Instance);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_RoutesEveryItemAndArchivesPackage()
    {
        // Arrange
        var package = new InboxPackage("package-1", "inbox/package-1");
        var passport = new PackagePassport([
            new PackageItem("low", "low.txt", "Low", null, 1, 10m),
            new PackageItem("high", "high.txt", "High", null, 1, 1_000_000m)
        ]);
        var routedChannels = new List<RouteChannel>();
        var operations = new List<string>();

        inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([package]);
        reader
            .Setup(service => service.ReadAsync(package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        writer
            .Setup(service => service.WriteAsync(package, It.IsAny<RoutingDecision>(), It.IsAny<CancellationToken>()))
            .Callback<InboxPackage, RoutingDecision, CancellationToken>((_, decision, _) =>
            {
                routedChannels.Add(decision.Item.RouteChannel);
                operations.Add($"write:{decision.Item.OrderId}");
            })
            .Returns(Task.CompletedTask);
        notifier
            .Setup(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()))
            .Callback<PackageItem, RouteChannel, CancellationToken>((item, _, _) =>
                operations.Add($"notify:{item.OrderId}"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PackagesProcessed);
        Assert.Equal(2, result.ItemsRouted);
        Assert.Equal(0, result.PackagesFailed);
        Assert.Equal([RouteChannel.LowPrice, RouteChannel.HighPrice], routedChannels);
        Assert.Equal(["notify:low", "write:low", "notify:high", "write:high"], operations);
        notifier.Verify(
            service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        archiver.Verify(service => service.ArchiveAsync(package, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenPassportFails_DoesNotArchivePackage()
    {
        // Arrange
        var package = new InboxPackage("bad-package", "inbox/bad-package");

        inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([package]);
        reader
            .Setup(service => service.ReadAsync(package, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("Invalid passport."));

        // Act
        var result = await router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result.PackagesProcessed);
        Assert.Equal(0, result.ItemsRouted);
        Assert.Equal(1, result.PackagesFailed);
        writer.Verify(
            service => service.WriteAsync(It.IsAny<InboxPackage>(), It.IsAny<RoutingDecision>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        archiver.Verify(service => service.ArchiveAsync(It.IsAny<InboxPackage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenNotificationFails_StillArchivesPackage()
    {
        // Arrange
        var package = new InboxPackage("package-1", "inbox/package-1");
        var passport = new PackagePassport([
            new PackageItem("order-1", "data.txt", "Data", null, 1, 10m)
        ]);

        inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([package]);
        reader
            .Setup(service => service.ReadAsync(package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        notifier
            .Setup(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Telegram is unavailable."));

        // Act
        var result = await router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PackagesProcessed);
        Assert.Equal(1, result.ItemsRouted);
        Assert.Equal(0, result.PackagesFailed);
        archiver.Verify(service => service.ArchiveAsync(package, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenOutgoingPackageAlreadyExists_SkipsNotifyAndWrite()
    {
        // Arrange
        var package = new InboxPackage("package-1", "processing/package-1");
        var passport = new PackagePassport([new PackageItem("order-1", "data.txt", "Data", null, 1, 10m)]);

        inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([package]);
        reader
            .Setup(service => service.ReadAsync(package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        writer
            .Setup(service => service.Exists(It.IsAny<RoutingDecision>()))
            .Returns(true);

        // Act
        var result = await router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PackagesProcessed);
        Assert.Equal(1, result.ItemsRouted);
        Assert.Equal(0, result.PackagesFailed);
        notifier
            .Verify(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
                Times.Never);
        writer
            .Verify(service => service
                    .WriteAsync(It.IsAny<InboxPackage>(), It.IsAny<RoutingDecision>(), It.IsAny<CancellationToken>()),
                Times.Never);
        archiver.Verify(service => service.ArchiveAsync(package, It.IsAny<CancellationToken>()), Times.Once);
    }
}