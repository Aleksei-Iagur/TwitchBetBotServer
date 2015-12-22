using System.Linq;
using System.Text;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Controllers
{
    class InventoryMessageController
    {
        private readonly IOptionsManager _optionsManager;
        private readonly IUsersManager _usersManager;
        private readonly IMessageSender _messageSender;

        public InventoryMessageController(IOptionsManager optionsManager, IUsersManager usersManager, IMessageSender messageSender)
        {
            _optionsManager = optionsManager;
            _usersManager = usersManager;
            _messageSender = messageSender;
        }

        public void Handle(string[] message, string username)
        {
            if (message[0].ToLower() != "!inventory" && message[0].ToLower() != "!i")
            {
                return;
            }

            var userId = _usersManager.GetUserId(username);
            var options = _optionsManager.GetUserOptions(userId);
            var sb = new StringBuilder(username + ", you have: ");

            if (options != null && options.Any())
            {
                foreach (var option in options)
                {
                    sb.AppendFormat("{0} (expires in {1} days); ", option.Option, option.ExpiresIn);
                }
            }
            else
            {
                sb.Append("a lot of free space for some cool stuff ;)");
            }

            _messageSender.Send(sb.ToString());
        }
    }
}
