using System;
using System.Collections.Generic;
using System.Text;
using PrismataTvServer.Classes;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Managers
{
    class ShopManager : IShopManager
    {
        private readonly ICurrencyManager _currencyManager;
        private readonly IMessageSender _messageSender;
        private readonly IUsersManager _usersManager;
        private List<ShopItem> _items;

        public ShopManager(ICurrencyManager currencyManager, IMessageSender messageSender, IUsersManager usersManager)
        {
            _currencyManager = currencyManager;
            _messageSender = messageSender;
            _usersManager = usersManager;
            InitItems();
        }

        private void InitItems()
        {
            _items = new List<ShopItem>
            {
                new ShopItem("GreetingsBot", $"That bot will greet you when you write first message in chat and tell your amount of {_currencyManager.CurrencyName} if you are not in top10 and can't see them on screen.", 35),
                new ShopItem("MinBetActivator","Activates an ability to place minimum bet by typing \"!bet min <player>\"", 50),
                new ShopItem("AllInActivator", "Activates an ability to go all-in by typing \"!bet all <player>\"", 50)
            };
        }

        public void ShowAllItems()
        {
            var sb = new StringBuilder();
            foreach (var item in _items)
            {
                sb.AppendFormat("{0} ({1} {2}) - {3}; ", item.Name, item.Price, _currencyManager.CurrencyName, item.Desc);
            }

            _messageSender.Send(sb.ToString());
        }

        public void BuyItem(int userId, string itemname)
        {
            var username = _usersManager.GetUserName(userId);
            var itemPrice = GetItemPrice(itemname);
            ViewerOptions option;
            
            switch (itemname.ToLower())
            {
                case "greetingsbot":
                    option = ViewerOptions.GreetingsBot;
                    break;
                case "minbetactivator":
                    option = ViewerOptions.MinBetActivator;
                    break;
                case "allinactivator":
                    option = ViewerOptions.AllInActivator;
                    break;
                default:
                    _messageSender.Send($"Item {itemname} wasn't recognized.");
                    return;
            }

            if (_currencyManager.GetUserCoins(userId) < itemPrice)
            {
                _messageSender.Send($"{username}, you haven't enough {_currencyManager.CurrencyName}. \"{option}\" costs {itemPrice} {_currencyManager.CurrencyName}.");
                return;
            }

            var result = _usersManager.BuyOption(username, option);
            if (!result)
            {
                _messageSender.Send($"{username}, you already have \"{option}\".");
            }
            else
            {
                _messageSender.Send($"Congratulation, {username}, now you have \"{option}\".");
                _currencyManager.RemoveCoinsFromUser(userId, itemPrice);
            }
        }

        private int GetItemPrice(string itemname)
        {
            return _items.Find(x => string.Equals(x.Name, itemname, StringComparison.CurrentCultureIgnoreCase)).Price;
        }
    }
}
