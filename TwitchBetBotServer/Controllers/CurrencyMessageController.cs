using System.Collections.Generic;
using System.Threading;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Controllers
{
    public class CurrencyMessageController
    {
        private readonly IUsersManager _usersManager;
        private readonly ICurrencyManager _currencyManager;

        public CurrencyMessageController(IUsersManager usersManager, ICurrencyManager currencyManager)
        {
            _usersManager = usersManager;
            _currencyManager = currencyManager;
        }

        public void Handle(string[] message, string user, List<string> usersToLookup, Timer currencyQueue)
        {
            switch (message.Length)
            {
                case 2:
                    _currencyManager.AddToLookups(user, usersToLookup, currencyQueue);
                    break;
                case 3:
                    if (_usersManager.GetUserLevel(user) != UserRoles.Admin)
                    {
                        break;
                    }

                    if (message[2] == "top")
                    {
                        _currencyManager.ShowTop10();
                    }
                    else
                    {
                        var username = message[2];
                        _currencyManager.CheckUserCurrency(username);
                    }
                    break;
                default:
                    if (message.Length < 3)
                    {
                        return;
                    }

                    int amount;
                    switch (message[2])
                    {
                        case "add":
                            if (_usersManager.GetUserLevel(user) < UserRoles.Moderator)
                            {
                                break;
                            }

                            if (int.TryParse(message[3], out amount) && message.Length >= 5)
                            {
                                if (message[4].Equals("all"))
                                {
                                    _currencyManager.AddCoinsToAllWithMessage(amount);
                                }
                                else
                                {
                                    _currencyManager.AddCoinsToUserWithMessage(message[4], amount);
                                }
                            }
                            break;

                        case "remove":
                            if (_usersManager.GetUserLevel(user) < UserRoles.Moderator)
                            {
                                break;
                            }

                            if (message[3] != null && int.TryParse(message[3], out amount) && message.Length >= 5)
                            {
                                if (message[4].Equals("all"))
                                {
                                    _currencyManager.RemoveCurrencyFromAll(amount, user);
                                }
                                else
                                {
                                    var username = message[4];
                                    _currencyManager.RemoveCurrencyFromUser(username, amount, user);
                                }

                            }
                            break;
                    }
                    break;
            } 
        }
    }
}
