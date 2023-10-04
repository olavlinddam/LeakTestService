using System.Text;
using LeakTestService.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace LeakTestService.Services;

public class RabbitMqProducer : IRabbitMqProducer
{
    private readonly RabbitMqConfig _config;

    public RabbitMqProducer(IOptions<RabbitMqConfig> options)
    {
        _config = options.Value;
    }

    public void SendLeakTestMessage<T>(T message)
    {
        var factory = new ConnectionFactory
        {
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,
            HostName = _config.HostName,
            Port = _config.Port,
            ClientProvidedName = _config.ClientProvidedName
        };
        
        var connection = factory.CreateConnection();
        
        // setting up the channel
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Direct);
        channel.QueueDeclare(_config.QueueName,false,false,false,null);
        // Bind the exchange and queue and provide the routing key needed to send the messages
        channel.QueueBind(_config.QueueName, _config.ExchangeName, _config.RoutingKey, null);
        
        // Preparing the message to be send
        var json = JsonConvert.SerializeObject(message, Formatting.Indented);
        var body = Encoding.UTF8.GetBytes(json);
        
        // Send the message to the queue
        channel.BasicPublish(_config.ExchangeName, _config.RoutingKey, null, body);
    }
}