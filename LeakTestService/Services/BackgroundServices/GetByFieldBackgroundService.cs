using LeakTestService.Configuration;
using LeakTestService.Services.Consumers;
using Microsoft.Extensions.Options;

namespace LeakTestService.Services.BackgroundServices;

public class GetByFieldBackgroundService : BackgroundService
{
    private readonly IMessageConsumer _getByFieldConsumer;


    public GetByFieldBackgroundService(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
    {
        _getByFieldConsumer = new GetByFieldConsumer(configOptions, serviceProvider);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            // Logging and cleanup logic here
        });

        // Start listening for messages
        _getByFieldConsumer.StartListening();

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _getByFieldConsumer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}