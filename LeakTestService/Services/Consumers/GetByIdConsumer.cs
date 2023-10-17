using System.Text;
using System.Text.Json;
using LeakTestService.Configuration;
using LeakTestService.Controllers;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LeakTestService.Services.Consumers;

public class GetByIdConsumer : IMessageConsumer
{
    private readonly LeakTestRabbitMqConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;
    private readonly IServiceProvider _serviceProvider;
    private const string queueName = "get-by-id-requests";
    private const string routingKey = "get-by-id-route";

    
    public GetByIdConsumer(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
    {
        _config = configOptions.Value;
        _serviceProvider = serviceProvider;
        
        var factory = new ConnectionFactory
        {
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,  
            HostName = _config.HostName,
            //Port = int.Parse(_config.Port),
            Port = 5672,
            ClientProvidedName = _config.ClientProvidedName
        };
        
         _connection = factory.CreateConnection();
         _channel = _connection.CreateModel();
        
         _channel.ExchangeDeclare("leaktest-exchange", ExchangeType.Direct, durable: true);
        
         _channel.QueueDeclare("get-by-id-requests", exclusive: false);
         _channel.QueueBind("get-by-id-requests", "leaktest-exchange", "get-by-id-route");

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

        _channel.BasicConsume(queue: "get-by-id-requests", autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
    
    private async Task<string> ProcessRequest(string requestMessage)
    {
        Guid id = Guid.Parse(requestMessage);
        
        // Creating a scope to access the controller
        using (var scope = _serviceProvider.CreateScope())
        {
            var leakTestHandler = scope.ServiceProvider.GetRequiredService<LeakTestHandler>();
            
            // Passing the message to the controller to get an ID of the created resource back
            var leakTestId = await leakTestHandler.GetById(id);
            
            var processedRequest = leakTestId.ToString();
            return processedRequest;

        }
    }
    
    
}