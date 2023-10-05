using System.Text;
using LeakTestService.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LeakTestService.Services;

public class ConsumerBackgroundService : BackgroundService
{
    private readonly IRabbitMqConsumer _consumer;

    public ConsumerBackgroundService(IOptions<LeakTestRabbitMqConfig> configOptions)
    {
        _consumer = new RabbitMqConsumer(configOptions);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Since our consumer is already listening after being initialized in the constructor, we dont need to do anything here.
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _consumer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}