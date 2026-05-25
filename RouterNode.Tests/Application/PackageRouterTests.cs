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
    private readonly Mock<IPackageInbox> _inbox;

    private readonly Mock<IPackagePassportReader> _reader;

    private readonly Mock<IOutgoingPackageWriter> _writer;

    private readonly Mock<IPackageArchiver> _archiver;

    private readonly Mock<IPackageDeadLetterStore> _deadLetterStore;

    private readonly Mock<IItemTransferNotifier> _notifier;

    private readonly PackageRouter _router;

    public PackageRouterTests()
    {
        _inbox = new Mock<IPackageInbox>();
        _reader = new Mock<IPackagePassportReader>();
        _writer = new Mock<IOutgoingPackageWriter>();
        _archiver = new Mock<IPackageArchiver>();
        _deadLetterStore = new Mock<IPackageDeadLetterStore>();
        _notifier = new Mock<IItemTransferNotifier>();
        IPackageRoutingPolicy routingPolicy1 = new PackageRoutingPolicy(new SafePackageFolderNamePolicy());
        _router = new PackageRouter(_inbox.Object, _reader.Object, _writer.Object, _archiver.Object,
            _deadLetterStore.Object, _notifier.Object, routingPolicy1, NullLogger<PackageRouter>.Instance);
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

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([package]);
        _reader
            .Setup(service => service.ReadAsync(package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        _writer
            .Setup(service => service.WriteAsync(package, It.IsAny<RoutingDecision>(), It.IsAny<CancellationToken>()))
            .Callback<InboxPackage, RoutingDecision, CancellationToken>((_, decision, _) =>
            {
                routedChannels.Add(decision.Item.RouteChannel);
                operations.Add($"write:{decision.Item.OrderId}");
            })
            .Returns(Task.CompletedTask);
        _notifier
            .Setup(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()))
            .Callback<PackageItem, CancellationToken>((item, _) => operations.Add($"notify:{item.OrderId}"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PackagesProcessed);
        Assert.Equal(2, result.ItemsRouted);
        Assert.Equal(0, result.PackagesFailed);
        Assert.Equal([RouteChannel.LowPrice, RouteChannel.HighPrice], routedChannels);
        Assert.Equal(["notify:low", "write:low", "notify:high", "write:high"], operations);
        _notifier.Verify(
            service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _archiver.Verify(service => service.ArchiveAsync(package, It.IsAny<CancellationToken>()), Times.Once);
        _deadLetterStore
            .Verify(service => service
                    .MoveAsync(It.IsAny<InboxPackage>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenPassportFails_DoesNotArchivePackage()
    {
        // Arrange
        var package = new InboxPackage("bad-package", "inbox/bad-package");

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([package]);
        _reader
            .Setup(service => service.ReadAsync(package, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("Invalid passport."));

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result.PackagesProcessed);
        Assert.Equal(0, result.ItemsRouted);
        Assert.Equal(1, result.PackagesFailed);
        _writer.Verify(
            service => service.WriteAsync(It.IsAny<InboxPackage>(), It.IsAny<RoutingDecision>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _archiver.Verify(service => service.ArchiveAsync(It.IsAny<InboxPackage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deadLetterStore
            .Verify(service => service
                    .MoveAsync(package, It.IsAny<InvalidDataException>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenNotificationFails_DoesNotWriteOrArchivePackage()
    {
        // Arrange
        var package = new InboxPackage("package-1", "inbox/package-1");
        var passport = new PackagePassport([
            new PackageItem("order-1", "data.txt", "Data", null, 1, 10m)
        ]);

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([package]);
        _reader
            .Setup(service => service.ReadAsync(package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        _notifier
            .Setup(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Telegram is unavailable."));

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result.PackagesProcessed);
        Assert.Equal(0, result.ItemsRouted);
        Assert.Equal(1, result.PackagesFailed);
        _writer
            .Verify(service => service.WriteAsync(It.IsAny<InboxPackage>(), It.IsAny<RoutingDecision>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        _archiver.Verify(service => service.ArchiveAsync(It.IsAny<InboxPackage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deadLetterStore
            .Verify(service => service
                    .MoveAsync(package, It.IsAny<HttpRequestException>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenPackageCannotBeWritten_DoesNotNotifyWriteOrArchivePackage()
    {
        // Arrange
        var package = new InboxPackage("package-1", "processing/package-1");
        var passport = new PackagePassport([new PackageItem("order-1", "missing.txt", "Data", null, 1, 10m)]);

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([package]);
        _reader
            .Setup(service => service.ReadAsync(package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        _writer
            .Setup(service => service.EnsureCanWrite(package, It.IsAny<IReadOnlyCollection<RoutingDecision>>()))
            .Throws(new FileNotFoundException("Attachment is missing."));

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result.PackagesProcessed);
        Assert.Equal(0, result.ItemsRouted);
        Assert.Equal(1, result.PackagesFailed);
        _notifier
            .Verify(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
                Times.Never);
        _writer
            .Verify(service => service.WriteAsync(It.IsAny<InboxPackage>(), It.IsAny<RoutingDecision>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        _archiver.Verify(service => service.ArchiveAsync(It.IsAny<InboxPackage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deadLetterStore
            .Verify(service => service
                    .MoveAsync(package, It.IsAny<FileNotFoundException>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenOutgoingPackageAlreadyExists_SkipsNotifyAndWrite()
    {
        // Arrange
        var package = new InboxPackage("package-1", "processing/package-1");
        var passport = new PackagePassport([new PackageItem("order-1", "data.txt", "Data", null, 1, 10m)]);

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([package]);
        _reader
            .Setup(service => service.ReadAsync(package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        _writer
            .Setup(service => service.IsAlreadyWritten(It.IsAny<RoutingDecision>()))
            .Returns(true);

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PackagesProcessed);
        Assert.Equal(0, result.ItemsRouted);
        Assert.Equal(0, result.PackagesFailed);
        _notifier
            .Verify(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
                Times.Never);
        _writer
            .Verify(service => service
                    .WriteAsync(It.IsAny<InboxPackage>(), It.IsAny<RoutingDecision>(), It.IsAny<CancellationToken>()),
                Times.Never);
        _archiver.Verify(service => service.ArchiveAsync(package, It.IsAny<CancellationToken>()), Times.Once);
        _deadLetterStore
            .Verify(service => service.MoveAsync(It.IsAny<InboxPackage>(), It.IsAny<Exception>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }
}