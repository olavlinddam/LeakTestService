using LeakTestService.Configuration;
using LeakTestService.Services.Consumers;
using Microsoft.Extensions.Options;

namespace LeakTestService.Services.BackgroundServices;

public class GetByTagBackgroundService : BackgroundService
{
    private readonly IMessageConsumer _getByTagConsumer;


    public GetByTagBackgroundService(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
    {
        _getByTagConsumer = new GetByTagConsumer(configOptions, serviceProvider);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            // Logging and cleanup logic here
        });

        // Start listening for messages
        _getByTagConsumer.StartListening();

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _getByTagConsumer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}