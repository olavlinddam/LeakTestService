using System.Text;
using LeakTestService.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LeakTestService.Services;

public class RabbitMqConsumer : IRabbitMqConsumer
{
    private readonly LeakTestRabbitMqConfig _config;

    public RabbitMqConsumer(LeakTestRabbitMqConfig config)
    {
        _config = config;
    }

    public async Task<string?> Listen()
    {
        var factory = new ConnectionFactory
        {
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,
            HostName = _config.HostName,
            Port = int.Parse(_config.Port),
            ClientProvidedName = _config.ClientProvidedName
        };

        var connection = factory.CreateConnection();

        // setting up the channel
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Direct);
        channel.QueueDeclare(_config.RequestQueue, false, false, false, null);
        // Bind the exchange and queue and provide the routing key needed to send the messages
        channel.QueueBind(_config.RequestQueue, _config.ExchangeName, _config.RoutingKey, null);

        string? message = null;
        // Setting up the consumer
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (sender, args) =>
        { 
            Task.Delay(TimeSpan.FromSeconds(3)).Wait(); // simulating some work that takes 5 seconds.
            var body = args.Body.ToArray();
         
            message = Encoding.UTF8.GetString(body);
             
            // Acknowledging that the message was received.
            channel.BasicAck(args.DeliveryTag, false);
            
            await Task.Yield();
            // Once this scope is done the message is gone. We need to process the message within this scope. 
            // If we had an exception, we can send a "NotAck", and the message will not leave the queue. 

        };

        return message;
    }

}