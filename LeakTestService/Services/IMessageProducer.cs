namespace LeakTestService.Services;

public interface IMessageProducer 
{
    public void SendMessage < T > (T message, string routingKey);
}