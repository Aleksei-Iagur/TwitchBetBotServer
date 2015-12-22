using System.Globalization;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Controllers
{
    public class BettingMessageController
    {
        private readonly IMessageSender _messageSender;
        private readonly IBettingManager _bettingManager;
        private readonly IUsersManager _usersManager;
        private readonly IOptionsManager _optionsManager;
        private readonly ICurrencyManager _currencyManager;

        public BettingMessageController(IMessageSender messageSender, IBettingManager bettingManager, IUsersManager usersManager, IOptionsManager optionsManager, ICurrencyManager currencyManager)
        {
            _messageSender = messageSender;
            _bettingManager = bettingManager;
            _usersManager = usersManager;
            _optionsManager = optionsManager;
            _currencyManager = currencyManager;
        }

        public void Handle(string[] message, string username)
        {
            switch (message[1])
            {
                case "!gamble":
                    if (message.Length < 2)
                    {
                        return;
                    }

                    if (_usersManager.GetUserLevel(username) >= UserRoles.Moderator)
                    {
                        switch (message[1])
                        {
                            case "open":
                            case "o":
                                _bettingManager.OpenBetting();
                                break;

                            case "close":
                            case "c":
                                _bettingManager.CloseBetting();
                                break;

                            case "winner":
                            case "w":
                                if (message.Length != 3) return;
                                int winIndex;
                                if (int.TryParse(message[2], out winIndex))
                                {
                                    _bettingManager.SetWinner((GameResult) winIndex);
                                }
                                else
                                {
                                    _messageSender.Send("Wrong winner index =(");
                                }
                                break;

                            case "cancel":
                                _bettingManager.CancelBetting();
                                break;
                        }
                    }
                    break;

                case "!bet":
                    if (message.Length == 2) // only "!bet" was typed
                    {
                        _bettingManager.ShowBetForUser(username);
                        return;
                    }

                    var userId = _usersManager.GetUserId(username);
                    switch (message[2])
                    {
                        case "help":
                            _bettingManager.ShowHelp();
                            break;
                        case "min":
                                if (_optionsManager.HasUserOption(userId, ViewerOptions.MinBetActivator))
                                {
                                    message[2] = _bettingManager.GetMinBetForUser(username).ToString(CultureInfo.InvariantCulture);
                                    goto default;
                                }
                                else
                                {
                                    _messageSender.Send($"{username}, you have no MinBetActivator. You can buy it for 50 {_currencyManager.CurrencyName} by typing \"!buy MinBetActivator\".");
                                }
                            break;
                        case "all":
                            if (_optionsManager.HasUserOption(userId, ViewerOptions.AllInActivator))
                            {
                                message[2] = (_bettingManager.GetUserBetAmount(username) + _usersManager.GetUserCoins(username)).ToString(CultureInfo.InvariantCulture);
                                goto default;
                            }
                            else
                            {
                                _messageSender.Send($"{username}, you have no AllInActivator. You can buy it for 50 {_currencyManager.CurrencyName} by typing \"!buy AllInActivator\".");
                            }
                            break;
                        default:
                            if (_bettingManager.BettingOpen && !_bettingManager.PoolLocked)
                            {
                                int betAmount;
                                
                                if (!int.TryParse(message[2], out betAmount))
                                {
                                    _messageSender.SendFormat("{0} your bet should be a number.", username);
                                    return;
                                }
                                
                                int betOn;
                                switch (message[3].ToLower())
                                {
                                    case "1":
                                    case "t":
                                    case "top":
                                    case "north":
                                    case "n":
                                        betOn = 0;
                                        break;
                                    case "2":
                                    case "b":
                                    case "bot":
                                    case "bottom":
                                    case "south":
                                    case "s":
                                        betOn = 1;
                                        break;
                                    case "3":
                                    case "d":
                                    case "draw":
                                        betOn = 2;
                                        break;
                                    default:
                                        _messageSender.SendFormat("{0} is not valid option. {1}, your bet was not accepted.", message[3], username);
                                        return;
                                }
                                
                                _bettingManager.PlaceBet(username, betAmount, betOn);
                            }
                            else
                            {
                                _messageSender.SendFormat(
                                    "{0}, you can't place bet now because betting pool is closed.", username);
                            }
                            break;
                    }
                    break;

                case "!bets":
                    if (_currencyManager.GetUserCoins(username) >= 3)
                    {
                        _currencyManager.RemoveCoinsFromUser(username, 3);
                        _bettingManager.ShowCurrentBets();
                    }
                    break;
            }
        }
    }
}
