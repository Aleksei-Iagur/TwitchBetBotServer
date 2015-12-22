namespace PrismataTvServer.Interfaces
{
    public interface IBusProxy
    {
        void SendMessage(string queueName, string message);
    }
}