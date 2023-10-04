namespace LeakTestService.Services;

public interface IRabbitMqProducer {
    public void SendLeakTestMessage < T > (T message);
}