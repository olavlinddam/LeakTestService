namespace LeakTestService.Services;

public interface IRabbitMqProducer {
    public void SendMessage < T > (T message);
}