using System;
using System.Collections.Generic;
using System.Linq;
using PrismataTvServer.Classes;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Managers
{
    public class AwardsManager
    {
        private readonly ICurrencyManager _currencyManager;
        private readonly IGamesManager _gamesManager;
        private readonly IMessageSender _messageSender;
        private readonly IUsersManager _usersManager;
        private readonly List<Bet> _bets;
        private readonly HashSet<int> _users;

        public AwardsManager(ICurrencyManager currencyManager, IGamesManager gamesManager, IMessageSender messageSender, IUsersManager usersManager)
        {
            _currencyManager = currencyManager;
            _gamesManager = gamesManager;
            _messageSender = messageSender;
            _usersManager = usersManager;
            _bets = new List<Bet>();
            _users = new HashSet<int>();
        }

        private void Init()
        {
            var games = _gamesManager.GetAllGames();
            games.ForEach(x =>
            {
                _bets.AddRange(x.Bets);
                x.Bets.ForEach(bet => _users.Add(bet.ViewerId));
            });
        }

        public void AwardViewersOfTheDay()
        {
            Init();
            AwardWinners();
        }

        private void AwardWinners()
        {
            if (!_gamesManager.GetAllGames().Any())
            {
                Console.WriteLine(@"There is no games yet.");
                return;
            }
            int awardValue;
            _messageSender.Send("=== Viewers Of The Day ===");
            var viewers = GetUsersWithMaxWinSum(out awardValue);
            GiveCoinsToWinners(viewers, 1);
            var viewersString = GetNamesStringForUsers(viewers);
            _messageSender.Send($"Award \"Maximum win amount\" ({awardValue}) and 1 PrismaCoin go to {viewersString}.");

            viewers = GetUsersWithMaxWinBets(out awardValue);
            GiveCoinsToWinners(viewers, 3);
            viewersString = GetNamesStringForUsers(viewers);
            _messageSender.Send($"Award \"Maximum win bets count\" ({awardValue}) and 3 {_currencyManager.CurrencyName} go to {viewersString}.");

            viewers = GetUsersWithMaxBetsCount(out awardValue);
            GiveCoinsToWinners(viewers, 5);
            viewersString = GetNamesStringForUsers(viewers);
            _messageSender.Send($"Award \"Maximum bets count\" ({awardValue}) and 5 {_currencyManager.CurrencyName} go to {viewersString}.");

            viewers = GetUsersWithMaxWrongBets(out awardValue);
            GiveCoinsToWinners(viewers, 7);
            viewersString = GetNamesStringForUsers(viewers);
            _messageSender.Send($"Award \"Maximum lost bets count\" ({awardValue}) and 7 {_currencyManager.CurrencyName} go to {viewersString}.");

            viewers = GetUsersWithMaxLostSum(out awardValue);
            GiveCoinsToWinners(viewers, 10);
            viewersString = GetNamesStringForUsers(viewers);
            _messageSender.Send($"Award \"Maximum loss amount\" ({awardValue}) and 10 {_currencyManager.CurrencyName} go to {viewersString}.");

            _messageSender.Send("Nicely done, guys! You are great! ;)");
        }

        private void GiveCoinsToWinners(List<int> viewers, int coins)
        {
            viewers.ForEach(userId => _currencyManager.AddCoinsToUser(userId, coins));
        }

        private string GetNamesStringForUsers(IEnumerable<int> usersWithMaxWinSum)
        {
            var names = usersWithMaxWinSum.Select(x => _usersManager.GetUserName(x));
            return names.Aggregate((a, b) => a + ", " + b);
        }

        private List<int> GetUsersWithMaxWrongBets(out int awardValue)
        {
            var users = new List<int>();
            var wrongBetsNumber = 0;
            foreach (var currentUserId in _users)
            {
                var wrongBetsForCurrentUser = GetWrongBetsForUser(currentUserId);
                if (wrongBetsForCurrentUser == wrongBetsNumber)
                {
                    users.Add(currentUserId);
                }
                else if (wrongBetsForCurrentUser > wrongBetsNumber)
                {
                    users.Clear();
                    users.Add(currentUserId);
                    wrongBetsNumber = wrongBetsForCurrentUser;
                }
            }

            awardValue = wrongBetsNumber;
            return users;
        }

        private List<int> GetUsersWithMaxWinBets(out int awardValue)
        {
            var users = new List<int>();
            var winBetsNumber = 0;
            foreach (var currentUserId in _users)
            {
                var winBetsForCurrentUser = GetWinBetsForUser(currentUserId);
                if (winBetsForCurrentUser == winBetsNumber)
                {
                    users.Add(currentUserId);
                }
                else if (winBetsForCurrentUser > winBetsNumber)
                {
                    users.Clear();
                    users.Add(currentUserId);
                    winBetsNumber = winBetsForCurrentUser;
                }
            }

            awardValue = winBetsNumber;
            return users;
        }

        private List<int> GetUsersWithMaxLostSum(out int awardValue)
        {
            var users = new List<int>();
            var lostSum = 0;
            foreach (var currentUserId in _users)
            {
                var lostSumForCurrentUser = GetLostSumForUser(currentUserId);
                if (lostSumForCurrentUser == lostSum)
                {
                    users.Add(currentUserId);
                }
                else if (lostSumForCurrentUser > lostSum)
                {
                    users.Clear();
                    users.Add(currentUserId);
                    lostSum = lostSumForCurrentUser;
                }
            }

            awardValue = lostSum;
            return users;
        }

        private List<int> GetUsersWithMaxWinSum(out int awardValue)
        {
            var users = new List<int>();
            var winSum = 0;
            foreach (var currentUserId in _users)
            {
                var winSumForCurrentUser = GetWinSumForUser(currentUserId);
                if (winSumForCurrentUser == winSum)
                {
                    users.Add(currentUserId);
                }
                else if (winSumForCurrentUser > winSum)
                {
                    users.Clear();
                    users.Add(currentUserId);
                    winSum = winSumForCurrentUser;
                }
            }

            awardValue = winSum;
            return users;
        }

        private List<int> GetUsersWithMaxBetsCount(out int awardValue)
        {
            var users = new List<int>();
            var betsNumber = 0;
            foreach (var currentUserId in _users)
            {
                var betsForCurrentUser = GetBetsCountForUser(currentUserId);
                if (betsForCurrentUser == betsNumber)
                {
                    users.Add(currentUserId);
                }
                else if (betsForCurrentUser > betsNumber)
                {
                    users.Clear();
                    users.Add(currentUserId);
                    betsNumber = betsForCurrentUser;
                }
            }

            awardValue = betsNumber;
            return users;
        }

        private int GetWrongBetsForUser(int userId)
        {
            return _bets.Count(x => x.ViewerId == userId && !x.IsWin);
        }

        private int GetWinBetsForUser(int userId)
        {
            return _bets.Count(x => x.ViewerId == userId && x.IsWin);
        }

        private int GetLostSumForUser(int userId)
        {
            return _bets.Where(x => x.ViewerId == userId && !x.IsWin).Sum(x => x.BetAmount);
        }

        private int GetWinSumForUser(int userId)
        {
            return _bets.Where(x => x.ViewerId == userId && x.IsWin).Sum(x => x.WinAmount - x.BetAmount);
        }

        private int GetBetsCountForUser(int userId)
        {
            return _bets.Count(x => x.ViewerId == userId);
        }
    }
}
