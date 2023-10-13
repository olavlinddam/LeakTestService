using System.Text;
using LeakTestService.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LeakTestService.Services;

public class MessageConsumer : IMessageConsumer
{
    private readonly LeakTestRabbitMqConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;

    public MessageConsumer(IOptions<LeakTestRabbitMqConfig> configOptions)
    {
        _config = configOptions.Value;
        
        var factory = new ConnectionFactory
        {
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,  
            HostName = _config.HostName,
            //Port = int.Parse(_config.Port),
            Port = 5671,
            ClientProvidedName = _config.ClientProvidedName
        };
        
         _connection = factory.CreateConnection();
         _channel = _connection.CreateModel();
        
         //_channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Direct);//
        
         _channel.QueueDeclare("leaktest-request-queue", exclusive: false);
    }
    

    public void StartListening()
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            Console.WriteLine($"Received request: {ea.BasicProperties.CorrelationId}");
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // Process the message
            var responseMessage = ProcessRequest(message);

            // Send the response back
            var responseBody = Encoding.UTF8.GetBytes(responseMessage);
            
            _channel.BasicPublish(
                exchange: "",
                routingKey: ea.BasicProperties.ReplyTo,
                basicProperties: null, // potentielt skal vi sende corr id med.
                body: responseBody
            );
            
            Console.WriteLine(responseMessage);

            //_channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queue: "leaktest-request-queue", autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
    
    private string ProcessRequest(string requestMessage)
    {
        // Your logic to process the request and generate a response
        return requestMessage;
    }
}