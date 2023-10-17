using LeakTestService.Configuration;
using LeakTestService.Services.Consumers;
using Microsoft.Extensions.Options;

namespace LeakTestService.Services.BackgroundServices;

public class AddSingleBackgroundService : BackgroundService
{
    private readonly IMessageConsumer _addSingleConsumer;


    public AddSingleBackgroundService(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
    {
        _addSingleConsumer = new AddSingleConsumer(configOptions, serviceProvider);
        _addSingleConsumer.StartListening();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            // Logging and cleanup logic here
        });

        // Start listening for messages
        _addSingleConsumer.StartListening();

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _addSingleConsumer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}