using LeakTestService.Configuration;
using LeakTestService.Services.Consumers;
using Microsoft.Extensions.Options;

namespace LeakTestService.Services.BackgroundServices;

public class GetByIdBackgroundService : BackgroundService
{
    private readonly IMessageConsumer _getByIdConsumer;


    public GetByIdBackgroundService(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
    {
        _getByIdConsumer = new GetByIdConsumer(configOptions, serviceProvider);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            // Logging and cleanup logic here
        });

        // Start listening for messages
        _getByIdConsumer.StartListening();

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _getByIdConsumer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}