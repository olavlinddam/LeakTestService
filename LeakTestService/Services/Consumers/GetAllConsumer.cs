using System.Text;
using System.Text.Json;
using FluentValidation;
using LeakTestService.Configuration;
using LeakTestService.Controllers;
using LeakTestService.Exceptions;
using LeakTestService.Models;
using LeakTestService.Models.DTOs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LeakTestService.Services.Consumers;

public class GetAllConsumer : IMessageConsumer
{
    private readonly LeakTestRabbitMqConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;
    private readonly IServiceProvider _serviceProvider;
    private const string queueName = "get-all-requests";
    private const string routingKey = "get-all-route";

    
    public GetAllConsumer(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
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
            ClientProvidedName = _config.ClientProvidedName
        };
        
         _connection = factory.CreateConnection();
         _channel = _connection.CreateModel();
        
         _channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Direct, durable: true);
        
         _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
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
            var responseMessage = await ProcessRequest();

            // Send the response back
            var responseBody = Encoding.UTF8.GetBytes(responseMessage);
            
            var replyProperties = _channel.CreateBasicProperties();
            replyProperties.CorrelationId = ea.BasicProperties.CorrelationId;

            _channel.BasicPublish(
                exchange: "",
                routingKey: ea.BasicProperties.ReplyTo,
                basicProperties: replyProperties,
                body: responseBody
            );
            
            Console.WriteLine(responseMessage);

            //_channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queueName, autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
    
    private async Task<string> ProcessRequest()
    {
        try
        {
            // Creating a scope to access the controller
            using var scope = _serviceProvider.CreateScope();
            var leakTestHandler = scope.ServiceProvider.GetRequiredService<LeakTestHandler>();
            
            // Passing the message to the controller to get an ID of the created resource back
            var leakTests = await leakTestHandler.GetAllAsync();
            
            return CreateApiResponse(200, leakTests.ToList(), null);
        }
        catch (NoMatchingDataException e)
        {
            Console.WriteLine($"No matching data: {e.Message}");
            return CreateApiResponse(404, null, e.Message);
        }
        catch (ValidationException e)
        {
            Console.WriteLine($"Validation failed: {e.Message}");
            return CreateApiResponse(400, null, e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
            return CreateApiResponse(500, null, e.Message);
        }
    
    }
    
    private static string CreateApiResponse(int statusCode, List<LeakTest> data, string errorMessage)
    {
        var apiResponse = new ApiResponse<List<LeakTest>>
        {
            StatusCode = statusCode,
            Data = data,
            ErrorMessage = errorMessage
        };

        return JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions { WriteIndented = true });
    }
    
    
}