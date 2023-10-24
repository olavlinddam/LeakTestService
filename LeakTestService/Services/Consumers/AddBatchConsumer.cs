using System.Text;
using System.Text.Json;
using LeakTestService.Configuration;
using LeakTestService.Controllers;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LeakTestService.Services.Consumers;

public class AddBatchConsumer : IMessageConsumer
{
    private readonly LeakTestRabbitMqConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;
    private readonly IServiceProvider _serviceProvider;
    private const string queueName = "add-batch-requests";
    private const string routingKey = "add-batch-route";

    
    public AddBatchConsumer(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
    {
        _config = configOptions.Value;
        _serviceProvider = serviceProvider;
        
        var factory = new ConnectionFactory
        {
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,  
            HostName = _config.HostName,
            Port = int.Parse(_config.Port),
            //Port = 5672,
            ClientProvidedName = _config.ClientProvidedName
        };
        
         _connection = factory.CreateConnection();
         _channel = _connection.CreateModel();
        
         _channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Direct, durable: true);
        
         _channel.QueueDeclare(queueName, exclusive: false);
         _channel.QueueBind(queueName, _config.ExchangeName, routingKey);

    }
    

    public void StartListening()
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            Console.WriteLine($"Received request: {ea.BasicProperties.CorrelationId}");
            
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // Process the message
            var responseMessage = await ProcessRequest(message);

            var stringBuilder = new StringBuilder();
            responseMessage.ForEach(id => stringBuilder.Append(id + ";"));
            
            // Send the response back
            var responseBody = Encoding.UTF8.GetBytes(stringBuilder.ToString());
            
            _channel.BasicPublish(
                exchange: "",
                routingKey: ea.BasicProperties.ReplyTo,
                basicProperties: null, // potentielt skal vi sende corr id med.
                body: responseBody
            );

            responseMessage.ForEach(id => Console.WriteLine(id));

            //_channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queueName, autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
    
    private async Task<List<string>> ProcessRequest(string requestMessage)
    {
        using var doc = JsonDocument.Parse(requestMessage);
        var formattedDoc= doc.RootElement.ToString();
        
        // Creating a scope to access the controller
        using (var scope = _serviceProvider.CreateScope())
        {
            var leakTestHandler = scope.ServiceProvider.GetRequiredService<LeakTestHandler>();
            
            // Passing the message to the controller to get an ID of the created resource back
            var leakTestIds = await leakTestHandler.AddBatchAsync(formattedDoc);

            var processedRequest = new List<string>();
            leakTestIds.ForEach(id => processedRequest.Add(id.ToString()));
            return processedRequest;
        }
    }
}