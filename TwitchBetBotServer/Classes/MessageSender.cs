using System;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Classes
{
    public class MessageSender : IMessageSender
    {
        private readonly IBusProxy _busProxy;

        public MessageSender(IBusProxy busProxy)
        {
            _busProxy = busProxy;
        }
        
        public void Send(string message, MessagePriority priority = MessagePriority.Normal)
        {
            string queueName;
            switch (priority)
            {
                case MessagePriority.High:
                    queueName = "command.chat.say.high";
                    break;
                case MessagePriority.Low:
                    queueName = "command.chat.say.low";
                    break;
                default:
                    queueName = "command.chat.say.normal";
                    break;
            }
            _busProxy.SendMessage(queueName, message);
        }
        
        public void SendFormat(string format, params object[] args)
        {
            if (format == null || args == null)
                throw new ArgumentNullException(format == null ? "format" : "args");
            Send(string.Format(null, format, args));
        }
    }
}
