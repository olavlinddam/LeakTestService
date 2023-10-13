using RabbitMQ.Client.Events;

namespace LeakTestService.Services;

public interface IMessageConsumer : IDisposable
{
    public void StartListening();
}