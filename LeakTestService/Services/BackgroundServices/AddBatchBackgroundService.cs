using LeakTestService.Configuration;
using LeakTestService.Services.Consumers;
using Microsoft.Extensions.Options;

namespace LeakTestService.Services.BackgroundServices;

public class AddBatchBackgroundService : BackgroundService
{
    private readonly IMessageConsumer _addBatchConsumer;


    public AddBatchBackgroundService(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
    {
        _addBatchConsumer = new AddBatchConsumer(configOptions, serviceProvider);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            // Logging and cleanup logic here
        });

        // Start listening for messages
        _addBatchConsumer.StartListening();

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _addBatchConsumer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}