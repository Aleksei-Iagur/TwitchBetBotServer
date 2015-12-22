using PrismataTvServer.Enums;

namespace PrismataTvServer.Interfaces
{
    public interface IMessageSender
    {
        void Send(string message, MessagePriority priority = MessagePriority.Normal);
        void SendFormat(string format, params object[] args);
    }
}
