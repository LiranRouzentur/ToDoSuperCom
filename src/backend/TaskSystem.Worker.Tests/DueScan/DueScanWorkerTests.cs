using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TaskSystem.Shared.Contracts.Events;
using TaskSystem.Worker.DueScan;
using TaskSystem.Worker.Messaging;

namespace TaskSystem.Worker.Tests.DueScan;

public class DueScanWorkerTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IRabbitMqPublisher> _mockPublisher;
    private readonly Mock<ILogger<DueScanWorker>> _mockLogger;

    public DueScanWorkerTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockPublisher = new Mock<IRabbitMqPublisher>();
        _mockLogger = new Mock<ILogger<DueScanWorker>>();
    }

    [Fact]
    public async Task ExecuteAsync_WithValidConfiguration_StartsSuccessfully()
    {
        // Arrange
        var connectionString = "Data Source=:memory:";
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection")).Returns(connectionString);
        _mockConfiguration.Setup(c => c.GetValue<int>("DueScan:IntervalSeconds", It.IsAny<int>())).Returns(5);
        _mockConfiguration.Setup(c => c.GetValue<int>("DueScan:BatchSize", It.IsAny<int>())).Returns(10);

        var worker = new DueScanWorker(_mockConfiguration.Object, _mockPublisher.Object, _mockLogger.Object);
        var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100); // Let it start
        cts.Cancel();
        await task;

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("DueScan worker started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithLowIntervalSeconds_UsesMinimumValue()
    {
        // Arrange
        var connectionString = "Data Source=:memory:";
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection")).Returns(connectionString);
        _mockConfiguration.Setup(c => c.GetValue<int>("DueScan:IntervalSeconds", It.IsAny<int>())).Returns(1); // Below minimum
        _mockConfiguration.Setup(c => c.GetValue<int>("DueScan:BatchSize", It.IsAny<int>())).Returns(10);

        var worker = new DueScanWorker(_mockConfiguration.Object, _mockPublisher.Object, _mockLogger.Object);
        var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await task;

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("below minimum")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithHighBatchSize_UsesMaximumValue()
    {
        // Arrange
        var connectionString = "Data Source=:memory:";
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection")).Returns(connectionString);
        _mockConfiguration.Setup(c => c.GetValue<int>("DueScan:IntervalSeconds", It.IsAny<int>())).Returns(5);
        _mockConfiguration.Setup(c => c.GetValue<int>("DueScan:BatchSize", It.IsAny<int>())).Returns(200); // Above maximum

        var worker = new DueScanWorker(_mockConfiguration.Object, _mockPublisher.Object, _mockLogger.Object);
        var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await task;

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("exceeds maximum")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DueScanWorker(null!, _mockPublisher.Object, _mockLogger.Object));
    }
}
