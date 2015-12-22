using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Controllers
{
    class ShopMessageController
    {
        private readonly IShopManager _shopManager;
        private readonly IUsersManager _usersManager;
        private readonly IMessageSender _messageSender;

        public ShopMessageController(IShopManager shopManager, IUsersManager usersManager, IMessageSender messageSender)
        {
            _shopManager = shopManager;
            _usersManager = usersManager;
            _messageSender = messageSender;
        }

        public void Handle(string[] message, string username)
        {
            if (string.Equals(message[0], "!shop"))
            {
                _shopManager.ShowAllItems();
                return;
            }

            if (message.Length == 1) return;

            var userId = _usersManager.GetUserId(username);
            switch (message[1].ToLower())
            {
                case "greetingsbot":
                case "minbetactivator":
                case "allinactivator":
                    _shopManager.BuyItem(userId, message[1]);
                    break;
                default:
                    _messageSender.SendFormat("{0}, item \"{1}\" was not recognized.", username, message[1]);
                    break;
            }
        }
    }
}
