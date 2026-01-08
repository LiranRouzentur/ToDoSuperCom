using RabbitMQ.Client;
using TaskSystem.Worker.DueScan;
using TaskSystem.Worker.Messaging;
using TaskSystem.Worker.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Register infrastructure
builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();

// Create connection using the factory (handles retries)
// We build a temporary provider here to resolve the factory and logger for startup operations
var sp = builder.Services.BuildServiceProvider();
var connectionFactory = sp.GetRequiredService<IRabbitMqConnectionFactory>();
var logger = sp.GetRequiredService<ILogger<Program>>();

IConnection connection;
try 
{
    connection = connectionFactory.CreateConnection();
    // Register the connection singleton for other services
    builder.Services.AddSingleton(connection);

    // Declare topology on startup
    using (var channel = connection.CreateModel())
    {
        var topologyManager = new RabbitMqTopologyManager(
            sp.GetRequiredService<ILogger<RabbitMqTopologyManager>>());
        topologyManager.DeclareTopology(channel);
    }
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Worker service failed to start due to RabbitMQ connectivity issues.");
    throw;
}

// Register publisher as singleton
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>(sp =>
    new RabbitMqPublisher(sp.GetRequiredService<IConnection>(), sp.GetRequiredService<ILogger<RabbitMqPublisher>>()));

// Register background workers
builder.Services.AddHostedService<DueScanWorker>();
builder.Services.AddHostedService<RabbitMqConsumer>();

var host = builder.Build();
host.Run();

