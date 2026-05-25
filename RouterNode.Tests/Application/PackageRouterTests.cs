using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RouterNode.Application.Packages;
using RouterNode.Domain.Entities;
using RouterNode.Domain.Notifications;
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

    private readonly OutgoingPackageDraft _draft;

    private readonly PackageRouter _router;

    private static readonly InboxPackage Package = new("package-1", "processing/package-1");

    public PackageRouterTests()
    {
        _inbox = new Mock<IPackageInbox>();
        _reader = new Mock<IPackagePassportReader>();
        _writer = new Mock<IOutgoingPackageWriter>();
        _archiver = new Mock<IPackageArchiver>();
        _deadLetterStore = new Mock<IPackageDeadLetterStore>();
        _notifier = new Mock<IItemTransferNotifier>();
        _draft = CreateDraft("order-1");
        IPackageRoutingPolicy routingPolicy = new PackageRoutingPolicy(new SafePackageFolderNamePolicy());
        _router = new PackageRouter(_inbox.Object, _reader.Object, _writer.Object, _archiver.Object,
            _deadLetterStore.Object, _notifier.Object, routingPolicy, NullLogger<PackageRouter>.Instance);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_RoutesEveryItemAndArchivesPackage()
    {
        // Arrange
        var passport = CreatePassport([
            CreatePackage("low", price: 10m),
            CreatePackage("high", price: 1_000_000m)
        ]);

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([Package]);
        _reader
            .Setup(service => service.ReadAsync(Package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        _writer
            .Setup(service => service.PrepareAsync(Package, It.IsAny<RoutingDecision>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_draft);
        _writer
            .Setup(service => service.PublishAsync(It.IsAny<OutgoingPackageDraft>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifier
            .Setup(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PackagesProcessed);
        Assert.Equal(2, result.ItemsRouted);
        Assert.Equal(0, result.PackagesFailed);
        _writer.Verify(service => service.PrepareAsync(Package, It.IsAny<RoutingDecision>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        _writer.Verify(service => service.PublishAsync(It.IsAny<OutgoingPackageDraft>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        _notifier
            .Verify(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        _archiver.Verify(service => service.ArchiveAsync(Package, It.IsAny<CancellationToken>()), Times.Once);
        _deadLetterStore
            .Verify(service => service.MoveAsync(It.IsAny<InboxPackage>(), It.IsAny<Exception>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenPassportFails_DoesNotArchivePackage()
    {
        // Arrange
        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([Package]);
        _reader
            .Setup(service => service.ReadAsync(Package, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("Invalid passport."));

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result.PackagesProcessed);
        Assert.Equal(0, result.ItemsRouted);
        Assert.Equal(1, result.PackagesFailed);
        _writer
            .Verify(service => service.PrepareAsync(It.IsAny<InboxPackage>(), It.IsAny<RoutingDecision>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        _archiver
            .Verify(service => service.ArchiveAsync(It.IsAny<InboxPackage>(), It.IsAny<CancellationToken>()),
                Times.Never);
        _deadLetterStore
            .Verify(service => service
                    .MoveAsync(Package, It.IsAny<InvalidDataException>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenNotificationFails_ArchivesPackage()
    {
        // Arrange
        var passport = CreatePassport([CreatePackage()]);

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([Package]);
        _reader
            .Setup(service => service.ReadAsync(Package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        _notifier
            .Setup(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Telegram is unavailable."));
        _writer
            .Setup(service => service.PrepareAsync(Package, It.IsAny<RoutingDecision>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_draft);
        _writer
            .Setup(service => service.PublishAsync(_draft, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PackagesProcessed);
        Assert.Equal(1, result.ItemsRouted);
        Assert.Equal(0, result.PackagesFailed);
        _writer
            .Verify(service => service.PrepareAsync(Package, It.IsAny<RoutingDecision>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        _writer.Verify(service => service.PublishAsync(_draft, It.IsAny<CancellationToken>()), Times.Once);
        _writer
            .Verify(service => service.RemoveTemporary(It.IsAny<OutgoingPackageDraft>()),
                Times.Never);
        _archiver.Verify(service => service.ArchiveAsync(Package, It.IsAny<CancellationToken>()), Times.Once);
        _deadLetterStore
            .Verify(service => service.MoveAsync(It.IsAny<InboxPackage>(), It.IsAny<Exception>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenPackageCannotBeWritten_DoesNotNotifyWriteOrArchivePackage()
    {
        // Arrange
        var passport = CreatePassport([CreatePackage(attachment: "missing.txt")]);

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([Package]);
        _reader
            .Setup(service => service.ReadAsync(Package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        _writer
            .Setup(service => service.EnsureCanWrite(Package, It.IsAny<IReadOnlyCollection<RoutingDecision>>()))
            .Throws(new FileNotFoundException("Attachment is missing."));

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result.PackagesProcessed);
        Assert.Equal(0, result.ItemsRouted);
        Assert.Equal(1, result.PackagesFailed);
        Times times = Times.Never();
        _notifier.Verify(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
            times);
        _writer
            .Verify(service => service.PrepareAsync(It.IsAny<InboxPackage>(), It.IsAny<RoutingDecision>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        _archiver.Verify(service => service.ArchiveAsync(It.IsAny<InboxPackage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deadLetterStore
            .Verify(service => service
                    .MoveAsync(Package, It.IsAny<FileNotFoundException>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenOutgoingPackageAlreadyExists_SkipsNotifyAndWrite()
    {
        // Arrange
        var passport = CreatePassport([CreatePackage()]);

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([Package]);
        _reader
            .Setup(service => service.ReadAsync(Package, It.IsAny<CancellationToken>()))
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
        Times times = Times.Never();
        _notifier.Verify(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
            times);
        _writer
            .Verify(service => service
                    .PrepareAsync(It.IsAny<InboxPackage>(), It.IsAny<RoutingDecision>(), It.IsAny<CancellationToken>()),
                Times.Never);
        _archiver.Verify(service => service.ArchiveAsync(Package, It.IsAny<CancellationToken>()), Times.Once);
        _deadLetterStore
            .Verify(service => service.MoveAsync(It.IsAny<InboxPackage>(), It.IsAny<Exception>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenPublishFails_DiscardsDraftAndMovesPackageToDeadLetter()
    {
        // Arrange
        var passport = CreatePassport([CreatePackage()]);

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([Package]);
        _reader
            .Setup(service => service.ReadAsync(Package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        _writer
            .Setup(service => service.PrepareAsync(Package, It.IsAny<RoutingDecision>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_draft);
        _writer
            .Setup(service => service.PublishAsync(_draft, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk is unavailable."));

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result.PackagesProcessed);
        Assert.Equal(0, result.ItemsRouted);
        Assert.Equal(1, result.PackagesFailed);
        _writer.Verify(service => service.RemoveTemporary(_draft),
            Times.Once);
        _notifier.Verify(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
            Times.Never());
        _archiver
            .Verify(service => service.ArchiveAsync(It.IsAny<InboxPackage>(), It.IsAny<CancellationToken>()),
                Times.Never);
        _deadLetterStore
            .Verify(service => service.MoveAsync(Package, It.IsAny<IOException>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenSecondPublishFails_RemovesPublishedItemAndDeadLettersPackage()
    {
        // Arrange
        var passport = CreatePassport([CreatePackage("first"), CreatePackage("second")]);
        var firstDraft = CreateDraft("first");
        var secondDraft = CreateDraft("second");

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([Package]);
        _reader
            .Setup(service => service.ReadAsync(Package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        _writer
            .Setup(service => service.PrepareAsync(Package, It.Is<RoutingDecision>(decision =>
                decision.Item.OrderId == "first"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstDraft);
        _writer
            .Setup(service => service.PrepareAsync(Package, It.Is<RoutingDecision>(decision =>
                decision.Item.OrderId == "second"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondDraft);
        _writer
            .Setup(service => service.PublishAsync(firstDraft, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _writer
            .Setup(service => service.PublishAsync(secondDraft, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk is unavailable."));

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result.PackagesProcessed);
        Assert.Equal(0, result.ItemsRouted);
        Assert.Equal(1, result.PackagesFailed);
        _writer.Verify(service => service.RemovePublished(firstDraft),
            Times.Once);
        _writer.Verify(service => service.RemoveTemporary(secondDraft),
            Times.Once);
        _notifier.Verify(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
            Times.Never());
        _archiver.Verify(service => service.ArchiveAsync(It.IsAny<InboxPackage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deadLetterStore
            .Verify(service => service.MoveAsync(Package, It.IsAny<IOException>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task ProcessReadyPackagesAsync_WhenArchiveFails_RemovesPublishedItemAndDeadLettersPackage()
    {
        // Arrange
        var passport = CreatePassport([CreatePackage()]);

        _inbox
            .Setup(service => service.GetReadyPackages())
            .Returns([Package]);
        _reader
            .Setup(service => service.ReadAsync(Package, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);
        _writer
            .Setup(service => service.PrepareAsync(Package, It.IsAny<RoutingDecision>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_draft);
        _writer
            .Setup(service => service.PublishAsync(_draft, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _archiver
            .Setup(service => service.ArchiveAsync(Package, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Archive is unavailable."));

        // Act
        var result = await _router.ProcessReadyPackagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result.PackagesProcessed);
        Assert.Equal(0, result.ItemsRouted);
        Assert.Equal(1, result.PackagesFailed);
        _writer
            .Verify(service => service.RemovePublished(_draft), Times.Once);
        _notifier.Verify(service => service.NotifyAsync(It.IsAny<PackageItem>(), It.IsAny<CancellationToken>()),
            Times.Never());
        _deadLetterStore
            .Verify(service => service.MoveAsync(Package, It.IsAny<IOException>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    private static PackagePassport CreatePassport(IReadOnlyList<PackageItem> items) => new(items);

    private static OutgoingPackageDraft CreateDraft(string orderId)
    {
        var decision = new RoutingDecision(CreatePackage(orderId), orderId);

        return new OutgoingPackageDraft(decision, $"temp/{orderId}", $"target/{orderId}");
    }

    private static PackageItem CreatePackage(string orderId = "order-1", string attachment = "data.txt",
        decimal price = 10m)
        => new(orderId, attachment, "Data", null, 1, price);
}
