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
        var inMemorySettings = new Dictionary<string, string> {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"DueScan:IntervalSeconds", "5"},
            {"DueScan:BatchSize", "10"}
        };
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var worker = new DueScanWorker(config, _mockPublisher.Object, _mockLogger.Object);
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
        var inMemorySettings = new Dictionary<string, string> {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"DueScan:IntervalSeconds", "1"}, // Below minimum
            {"DueScan:BatchSize", "10"}
        };
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var worker = new DueScanWorker(config, _mockPublisher.Object, _mockLogger.Object);
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
        var inMemorySettings = new Dictionary<string, string> {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"DueScan:IntervalSeconds", "5"},
            {"DueScan:BatchSize", "2000"} // Above maximum
        };
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var worker = new DueScanWorker(config, _mockPublisher.Object, _mockLogger.Object);
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
