using System;
using System.Collections.Generic;
using System.Linq;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Controllers
{
    public class OptionsController
    {
        private readonly IOptionsManager _optionsManager;
        private readonly IMessageSender _messageSender;
        private readonly ICurrencyManager _currencyManager;
        private readonly IUsersManager _usersManager;
        private readonly HashSet<string> _usersWithMessages; 

        public OptionsController(IOptionsManager optionsManager, IMessageSender messageSender, ICurrencyManager currencyManager, IUsersManager usersManager)
        {
            _optionsManager = optionsManager;
            _messageSender = messageSender;
            _currencyManager = currencyManager;
            _usersManager = usersManager;
            _usersWithMessages = new HashSet<string>();
        }

        public void CheckUserForGreetings(string username)
        {
            if (_usersWithMessages.Contains(username)) return;
            _usersWithMessages.Add(username);

            var userId = _usersManager.GetUserId(username);
            var hasOption = _optionsManager.HasUserOption(userId, ViewerOptions.GreetingsBot);
            if (!hasOption) return;

            var isInTop10 = _currencyManager.GetTop10().Any(x => x.Name.Equals(username, StringComparison.OrdinalIgnoreCase));

            var greetings = isInTop10
                ? $"Hey, {username}! Nice to see you! You are in Top10, congratulations! You are Great! Have a good time ;)"
                : $"Hi, {username}! You have {_currencyManager.GetUserCoins(username)} {_currencyManager.CurrencyName}. Good luck! :)";

            _messageSender.Send(greetings);
        }
    }
}
