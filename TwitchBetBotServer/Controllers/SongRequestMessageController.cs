using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Controllers
{
    class SongRequestMessageController
    {
        private readonly IMessageSender _messageSender;
        private readonly ICurrencyManager _currencyManager;

        public SongRequestMessageController(IMessageSender messageSender, ICurrencyManager currencyManager)
        {
            _messageSender = messageSender;
            _currencyManager = currencyManager;
        }

        public void Handle(string[] message, string username)
        {
            if (message.Length == 1) return;

            switch (message[1].ToLower())
            {
                default:
                    const int cost = 3;
                    _currencyManager.RemoveCoinsFromUser(username, cost);
                    _messageSender.Send($"{username}, {cost} {_currencyManager.CurrencyName} have been withdrawn for songrequest.");
                    break;
            }
        }
    }
}
