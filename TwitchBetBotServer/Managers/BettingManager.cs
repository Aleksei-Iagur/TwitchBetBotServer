using System;
using System.Collections.Generic;
using System.Linq;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Managers
{
    public class BettingManager : IBettingManager
    {
        private readonly IMessageSender _messageSender;
        private readonly IUsersManager _usersManager;
        private readonly IGamesManager _gamesManager;
        private readonly ICurrencyManager _currencyManager;

        public bool BettingOpen { get; set; }

        public bool PoolLocked { get; set; }
        
        public BettingManager(IMessageSender messageSender, IUsersManager usersManager, IGamesManager gamesManager, ICurrencyManager currencyManager)
        {
            _messageSender = messageSender;
            _usersManager = usersManager;
            _gamesManager = gamesManager;
            _currencyManager = currencyManager;
        }

        public void CloseBetting()
        {
            if (BettingOpen && !PoolLocked)
            {
                PoolLocked = true;
                _gamesManager.SaveGame();
                _messageSender.Send("Bets locked in.  Good luck everyone!", MessagePriority.High);
                ShowCurrentBets();
            }
            else
                _messageSender.Send("No pool currently open.");
        }

        public void OpenBetting()
        {
            if (!BettingOpen)
            {
                BettingOpen = true;
                _messageSender.Send("=== NEW BETTING POOL OPENED! ===", MessagePriority.High);
            }
            else
                _messageSender.Send(
                    "Betting Pool already opened.  Close or cancel the current one before starting a new one.",
                    MessagePriority.High);
        }

        public void CancelBetting()
        {
            _gamesManager.CancelCurrentGame();
            BettingOpen = false;
            PoolLocked = false;
            _messageSender.Send("Betting Pool canceled.  All bets refunded");
        }

        public void SetWinner(GameResult gameResult)
        {
            if (BettingOpen && PoolLocked)
            {
                _gamesManager.CloseGame(gameResult);

                BettingOpen = false;
                PoolLocked = false;
                _messageSender.Send($"Betting Pool closed! A total of {_gamesManager.GetTotalBetsForClosedGame()} {_currencyManager.CurrencyName} were bet.");

                ShowCurrentBets();

                var winners = _gamesManager.GetWinners();
                var output = "Winners:";
                if (!winners.Any())
                {
                    _messageSender.Send(output + " No One!");
                }

                var winnersCounter = 0;
                foreach (var winner in winners)
                {
                    winnersCounter++;
                    output += $" {winner.Name} - {winner.WinAmount} (Bet {winner.BetAmount})";

                    if (winnersCounter%10 != 0) continue;

                    _messageSender.Send(output);
                    output = "";
                }

                if (!string.IsNullOrWhiteSpace(output) && !output.Equals("Winners:", StringComparison.OrdinalIgnoreCase))
                {
                    _messageSender.Send(output);
                }
            }
            else
            {
                _messageSender.Send(
                    "Betting pool must be open and bets must be locked before you can specify a winner.");
            }
        }

        public void ShowCurrentBets()
        {
            if (!BettingOpen && !PoolLocked) return;

            var output = "Bets for:";
            for (var i = 0; i < 3; i++)
            {
                var x = _gamesManager.GetTotalBetsForCurrentGame() != 0 ? ((double)_gamesManager.GetTotalBetsOn((GameResult)i) / _gamesManager.GetTotalBetsForCurrentGame()) * 100 : 0;
                output += " " + _gamesManager.GetOptionName(i) + " - " + _gamesManager.GetNumberOfBets((GameResult)i) + " (" + _gamesManager.GetTotalBetsOn((GameResult)i) + "; " + Math.Round(x) + "%);";
            }
            _messageSender.Send(output);
        }

        public void ShowHelp()
        {
            var temp = "Betting options are: ";
            for (var i = 0; i < 3; i++)
            {
                temp += "(" + (i + 1) + ") " + _gamesManager.GetOptionName(i) + " ";
            }
            _messageSender.Send(temp, MessagePriority.Low);

            _messageSender.Send(
                "Bet by typing \"!bet 50 top\" to bet 50 " + _currencyManager.CurrencyName +
                " on Top player,  \"!bet 25 bot\" to bet 25 on Bottom player, also you can type \"!bet 15 draw\" to bet 15 on draw.",
                MessagePriority.Low);
        }
        
        public void PlaceBet(string username, int amount, GameResult option)
        {
            var userCoins = _usersManager.GetUserCoins(username) + _gamesManager.GetBetAmount(_usersManager.GetUserId(username));
            if (userCoins < amount)
            {
                _messageSender.SendFormat("{0}, you have {1} {2} and you can't place {3} {2} bet.", username, userCoins, _currencyManager.CurrencyName, amount);
                return;
            }

            if (amount < 1)
            {
                _messageSender.SendFormat("{0}, your bet ({1}) must be more then zero.", username, amount);
                return;
            }

            var minBet = GetMinBetForUser(username);
            if (amount < minBet)
            {
                _messageSender.SendFormat(
                    "{0}, your minimum bet should not be lower than {1} {2}.", username, minBet, _currencyManager.CurrencyName);
                return;
            }
            
            if (BettingOpen)
            {
                var userId = _usersManager.GetUserByName(username).Id;
                _gamesManager.PlaceBet(userId, option, amount);
            }
            else
            {
                _messageSender.SendFormat("{0}, betting pool is closed, your bet wasn't accepted.", username);
            }
        }

        public void PlaceBet(string username, int amount, int option)
        {
            PlaceBet(username, amount, (GameResult) option);
        }

        public int GetMinBetForUser(string username)
        {
            var userId = _usersManager.GetUserId(username);
            return GetMinBetForUser(userId);
        }

        public int GetMinBetForUser(int userId)
        {
            return (int)Math.Ceiling((double)(_usersManager.GetUserCoins(userId) + GetUserBetAmount(userId)) / 10);
        }

        public string GetUserBetOn(string username)
        {
            var userId = _usersManager.GetUserId(username);
            return _gamesManager.UserHasBet(userId) ? _gamesManager.GetBetOn(userId) : "-";
        }

        public void ShowBetForUser(string username)
        {
            var userId = _usersManager.GetUserId(username);
            if (_gamesManager.UserHasBet(userId))
            {
                _messageSender.SendFormat("{0}: {1} ({2})", username, _gamesManager.GetBetOn(userId),
                    _gamesManager.GetBetAmount(userId));
            }
            else
            {
                _messageSender.SendFormat("{0} has no bets now.", username);
            }
        }

        public bool IsUserInPool(string username)
        {
            var userId = _usersManager.GetUserId(username);
            return _gamesManager.UserHasBet(userId);
        }

        public int GetUserBetAmount(string username)
        {
            var userId = _usersManager.GetUserId(username);
            return GetUserBetAmount(userId);
        }

        public int GetUserBetAmount(int userId)
        {
            return _gamesManager.GetBetAmount(userId);
        }

        public List<string> GetCurrentBets()
        {
            if (!BettingOpen && !PoolLocked)
            {
                return new List<string>{"", "          Betting is closed now."};
            }

            var bets = new List<string> {""};
            var totalBets = _gamesManager.GetTotalBetsForCurrentGame();
            var betsPercent = totalBets == 0
                ? 0
                : Math.Round((double)(_gamesManager.GetTotalBetsOn(0))/totalBets * 100);
            var topPlayer =
                $"     Top player bets: {_gamesManager.GetNumberOfBets(GameResult.TopPlayerWin)} - {_gamesManager.GetTotalBetsOn(GameResult.TopPlayerWin)} ({betsPercent}%)";

            var betsPercentForBottom = totalBets == 0 ? 0 : 100 - betsPercent; 
            var bottomPlayer =
                $"     Bottom player bets: {_gamesManager.GetNumberOfBets(GameResult.BottomPlayerWin)} - {_gamesManager.GetTotalBetsOn(GameResult.BottomPlayerWin)} ({betsPercentForBottom}%)";

            bets.Add(topPlayer);
            bets.Add(bottomPlayer);
            return bets;
        }

        public void DeletePool()
        {
            BettingOpen = false;
            PoolLocked = false;
        }

        public void CreateNewGame()
        {
            _gamesManager.CreateNewGame();
        }

        public void UpdateGamePlayers(string player1, string player2)
        {
            _gamesManager.UpdateGamePlayers(player1, player2);
        }
    }
}
