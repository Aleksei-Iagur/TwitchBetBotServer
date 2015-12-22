using System;
using Apache.NMS;
using Apache.NMS.Util;
using Common.Logging;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Classes
{
    public class BusProxy : IBusProxy
    {
        private static readonly ILog Logger = LogManager.GetLogger<BusProxy>();
        private readonly IConnectionFactory _connectionFactory;

        public BusProxy(string busAddress)
        {
            var uri = new Uri(busAddress);
            _connectionFactory = new NMSConnectionFactory(uri);
        }

        public void SendMessage(string queueName, string message)
        {
            Logger.Trace(m => m("Message {0} has been sent to queue {1}.", message, queueName));
            using (var connection = _connectionFactory.CreateConnection())
            using (var session = connection.CreateSession())
            {
                var destination = SessionUtil.GetDestination(session, "queue://" + queueName);

                using (var producer = session.CreateProducer(destination))
                {
                    connection.Start();
                    producer.DeliveryMode = MsgDeliveryMode.Persistent;

                    var request = session.CreateTextMessage(message);

                    producer.Send(request);
                }
            }
        }
    }
}
