using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using TaskSystem.Shared.Contracts.Events;
using TaskSystem.Worker.Messaging;

namespace TaskSystem.Worker.Tests.Messaging;

public class RabbitMqPublisherTests
{
    private readonly Mock<IConnection> _mockConnection;
    private readonly Mock<IModel> _mockChannel;
    private readonly Mock<ILogger<RabbitMqPublisher>> _mockLogger;

    public RabbitMqPublisherTests()
    {
        _mockConnection = new Mock<IConnection>();
        _mockChannel = new Mock<IModel>();
        _mockLogger = new Mock<ILogger<RabbitMqPublisher>>();

        _mockConnection.Setup(c => c.CreateModel()).Returns(_mockChannel.Object);
        _mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new Mock<IBasicProperties>().Object);
    }

    [Fact]
    public void PublishTaskDue_WithValidMessage_PublishesSuccessfully()
    {
        // Arrange
        var publisher = new RabbitMqPublisher(_mockConnection.Object, _mockLogger.Object);
        var message = new TaskDueV1
        {
            TaskId = Guid.NewGuid(),
            Title = "Test Task",
            DueDateUtc = DateTime.UtcNow.AddDays(-1),
            TimestampUtc = DateTime.UtcNow
        };

        // Act
        publisher.PublishTaskDue(message);

        // Assert
        _mockChannel.Verify(
            c => c.BasicPublish(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>()),
            Times.Once);
    }

    [Fact]
    public void PublishTaskDue_WithChannelException_LogsErrorAndDoesNotThrow()
    {
        // Arrange
        _mockChannel.Setup(c => c.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()))
            .Throws(new Exception("RabbitMQ connection lost"));

        var publisher = new RabbitMqPublisher(_mockConnection.Object, _mockLogger.Object);
        var message = new TaskDueV1
        {
            TaskId = Guid.NewGuid(),
            Title = "Test Task",
            DueDateUtc = DateTime.UtcNow.AddDays(-1),
            TimestampUtc = DateTime.UtcNow
        };

        // Act
        var act = () => publisher.PublishTaskDue(message);

        // Assert
        act.Should().NotThrow();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to publish")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RabbitMqPublisher(null!, _mockLogger.Object));
    }
}
