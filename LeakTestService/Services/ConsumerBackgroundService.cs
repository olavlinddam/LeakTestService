using System.Text;
using LeakTestService.Configuration;
using LeakTestService.Controllers;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LeakTestService.Services;

public class ConsumerBackgroundService : BackgroundService
{
    private readonly IMessageConsumer _messageConsumer;


    public ConsumerBackgroundService(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
    {
        _messageConsumer = new MessageConsumer(configOptions, serviceProvider);
        _messageConsumer.StartListening();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            // Logging and cleanup logic here
        });

        // Start listening for messages
        _messageConsumer.StartListening();

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _messageConsumer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}