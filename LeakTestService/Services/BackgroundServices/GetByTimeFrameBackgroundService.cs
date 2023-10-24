using LeakTestService.Configuration;
using LeakTestService.Services.Consumers;
using Microsoft.Extensions.Options;

namespace LeakTestService.Services.BackgroundServices;

public class GetWithinTimeFrameBackgroundService : BackgroundService
{
    private readonly IMessageConsumer _getWithinTimeFrameConsumer;


    public GetWithinTimeFrameBackgroundService(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
    {
        _getWithinTimeFrameConsumer = new GetWithinTimeFrameConsumer(configOptions, serviceProvider);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            // Logging and cleanup logic here
        });

        // Start listening for messages
        _getWithinTimeFrameConsumer.StartListening();

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _getWithinTimeFrameConsumer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}