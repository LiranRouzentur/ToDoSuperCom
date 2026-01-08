using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace TaskSystem.Worker.Infrastructure;

public interface IRabbitMqConnectionFactory
{
    IConnection CreateConnection();
}

public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private readonly ConnectionFactory _factory;
    private const int MaxRetries = 5;
    private readonly TimeSpan _initialRetryDelay = TimeSpan.FromSeconds(2);

    public RabbitMqConnectionFactory(IConfiguration configuration, ILogger<RabbitMqConnectionFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var rabbitConfig = _configuration.GetSection("RabbitMq");
        _factory = new ConnectionFactory
        {
            HostName = rabbitConfig["Host"] ?? "localhost",
            UserName = rabbitConfig["Username"] ?? "guest",
            Password = rabbitConfig["Password"] ?? "guest",
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };
    }

    public IConnection CreateConnection()
    {
        var retryDelay = _initialRetryDelay;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var connection = _factory.CreateConnection();
                _logger.LogInformation("Successfully connected to RabbitMQ on attempt {Attempt}", attempt);
                return connection;
            }
            catch (BrokerUnreachableException ex)
            {
                LogRetry(ex, attempt, retryDelay);
            }
            catch (Exception ex)
            {
                LogRetry(ex, attempt, retryDelay);
            }

            if (attempt < MaxRetries)
            {
                Thread.Sleep(retryDelay);
                retryDelay = TimeSpan.FromSeconds(retryDelay.TotalSeconds * 2); // Exponential backoff
            }
        }

        throw new InvalidOperationException($"Failed to connect to RabbitMQ after {MaxRetries} attempts.");
    }

    private void LogRetry(Exception ex, int attempt, TimeSpan delay)
    {
        _logger.LogWarning(ex, "Failed to connect to RabbitMQ (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}s...", 
            attempt, MaxRetries, delay.TotalSeconds);
    }
}
