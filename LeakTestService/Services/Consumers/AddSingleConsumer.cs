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

public class AddSingleConsumer : IMessageConsumer
{
    private readonly LeakTestRabbitMqConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;
    private readonly IServiceProvider _serviceProvider;
    public AddSingleConsumer(IOptions<LeakTestRabbitMqConfig> configOptions, IServiceProvider serviceProvider)
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
        
         _channel.QueueDeclare(queue: "add-single-requests", durable: true, exclusive: false, autoDelete: false, arguments: null);
         _channel.QueueBind("add-single-requests", "leaktest-exchange", "add-single-route");

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

        _channel.BasicConsume(queue: "add-single-requests", autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
    
    private async Task<string> ProcessRequest(string requestMessage)
    {
        try
        {
            using var doc = JsonDocument.Parse(requestMessage);
            var formattedDoc= doc.RootElement.ToString();
        
            // Creating a scope to access the controller
            using var scope = _serviceProvider.CreateScope();
            var leakTestHandler = scope.ServiceProvider.GetRequiredService<LeakTestHandler>();
            
            // Passing the message to the controller to get an ID of the created resource back
            var leakTest = await leakTestHandler.AddSingleAsync(formattedDoc);

            return CreateApiResponse(200, leakTest, null);
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
    
    private static string CreateApiResponse(int statusCode, LeakTest data, string errorMessage)
    {
        var apiResponse = new ApiResponse<LeakTest>
        {
            StatusCode = statusCode,
            Data = data,
            ErrorMessage = errorMessage
        };

        return JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions { WriteIndented = true });
    }
    
    
}