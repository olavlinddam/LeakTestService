namespace LeakTestService.Services;

public interface IRabbitMqConsumer
{
    public Task<string?> Listen();
}