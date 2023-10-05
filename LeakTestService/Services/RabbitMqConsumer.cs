using System.Text;
using LeakTestService.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LeakTestService.Services;

public class RabbitMqConsumer : IRabbitMqConsumer
{
    private readonly LeakTestRabbitMqConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;

    public RabbitMqConsumer(IOptions<LeakTestRabbitMqConfig> configOptions)
    {
        _config = configOptions.Value;
        
        var factory = new ConnectionFactory
        {
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,
            HostName = _config.HostName,
            Port = int.Parse(_config.Port),
            ClientProvidedName = _config.ClientProvidedName
        };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Direct);
        _channel.QueueDeclare(_config.RequestQueue, false, false, false, null);
        // Bind the exchange and queue and provide the routing key needed to send the messages
        _channel.QueueBind(_config.RequestQueue, _config.ExchangeName, _config.RoutingKey, null);

        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += HandleReceivedMessage;
        
        _channel.BasicConsume(_config.RequestQueue, false, _consumer);
    }
    

    private void HandleReceivedMessage(object? sender, BasicDeliverEventArgs args)
    {
        var body = args.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        Console.WriteLine(message);

        // Behandl beskeden her eller send den til en anden metode eller klasse for behandling

        _channel.BasicAck(args.DeliveryTag, false);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }

}