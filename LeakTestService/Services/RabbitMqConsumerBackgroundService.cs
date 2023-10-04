using System.Text;
using LeakTestService.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LeakTestService.Services;

public class RabbitMqConsumerBackgroundService : BackgroundService
{

    private readonly IRabbitMqConsumer _consumer;

    public RabbitMqConsumerBackgroundService(IRabbitMqConsumer consumer)
    {
        _consumer = consumer;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Denne metode kører, når din service starter
        while (!stoppingToken.IsCancellationRequested)
        {
            // Her kan du lytte til RabbitMQ beskeder
            // For eksempel:

            var message = await _consumer.Listen(); // Dette er en fiktiv metode, du skal implementere den

            if (message != null)
            {
                // Behandl beskeden
                // HandleMessage(message);
            }

            // Vent et øjeblik før næste iteration for at undgå at overbelaste CPU'en
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}