using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PrismataTvServer.Classes;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Managers
{
    public class CurrencyManager : ICurrencyManager
    {
        public string CurrencyName { get; }
        private const int MaxAutoCoins = 20;
        private const int HandOutAmount = 1;

        private readonly IMessageSender _messageSender;
        private readonly IUsersManager _usersManager;
        private readonly IDatabaseManager _databaseManager;

        public CurrencyManager(IMessageSender messageSender, IUsersManager usersManager, IDatabaseManager databaseManager, string currencyName)
        {
            _messageSender = messageSender;
            _usersManager = usersManager;
            _databaseManager = databaseManager;
            CurrencyName = currencyName;
        }

        public void AddToLookups(string user, IList<string> usersToLookup, Timer currencyQueueTimer)
        {
            if (usersToLookup.Count == 0)
            {
                currencyQueueTimer.Change(4000, Timeout.Infinite);
            }
            if (!usersToLookup.Contains(user))
            {
                usersToLookup.Add(user);
            }
        }

        public void ShowTop10()
        {
            var top10 = GetTop10().Aggregate("Top 10: ", (current, user) => current + user.Name + " [" + user.Coins + "]; ");
            _messageSender.Send(top10);
        }

        private string CapName(string user)
        {
            return char.ToUpper(user[0]) + user.Substring(1);
        }

        public void CheckUserCurrency(string username)
        {
            if (_usersManager.UserExists(CapName(username)))
            {
                _messageSender.SendFormat("Admin check: {0} has {1} {2}", CapName(username), GetUserCoins(CapName(username)), CurrencyName);
            }
            else
                _messageSender.SendFormat("Admin check: {0} is not a valid user.", CapName(username));
        }

        private void Log(string output)
        {
            try
            {
                var log = new StreamWriter("CommandLog.log", true);
                log.WriteLine("<" + DateTime.Now + "> " + output);
                log.Close();
            }
            catch (Exception e)
            {
                var errorLog = new StreamWriter("Error_Log.log", true);
                errorLog.WriteLine("*************Error Message (via Log()): " + DateTime.Now + "*********************************");
                errorLog.WriteLine(e);
                errorLog.WriteLine("");
                errorLog.Close();
            }
        }

        public void AddCoinsToUserWithMessage(string username, int amount)
        {
            AddCoinsToUser(CapName(username), amount);
            _messageSender.Send("Added " + amount + " " + CurrencyName + " to " + CapName(username), MessagePriority.Low);
            Log("Added " + amount + " " + CurrencyName + " to " + CapName(username));
        }

        public void AddCoinsToAllWithMessage(int amount)
        {
            AddCoinsToAllOnline(amount);
            _messageSender.Send("Added " + amount + " " + CurrencyName + " to everyone.", MessagePriority.Low);
            Log("Added " + amount + " " + CurrencyName + " to everyone.");
        }

        public void RemoveCurrencyFromAll(int amount, string operatorUser)
        {
            RemoveCoinsFromAllOnline(amount);

            _messageSender.Send("Removed " + amount + " " + CurrencyName + " from everyone.", MessagePriority.Low);
            Log(operatorUser + " removed " + amount + " " + CurrencyName + " from everyone.");
        }

        public void RemoveCoinsFromUser(int userId, int coins)
        {
            AddCoinsToUser(userId, -coins);
        }

        public void RemoveCurrencyFromUser(string username, int amount, string operatorUser)
        {
            RemoveCoinsFromUser(CapName(username), amount);
            _messageSender.Send("Removed " + amount + " " + CurrencyName + " from " + CapName(username), MessagePriority.Low);
            Log(operatorUser + " removed " + amount + " " + CurrencyName + " from " + CapName(username));
        }

        public List<User> GetTop10()
        {
            return _usersManager.GetAllUsers().OrderByDescending(x => x.Coins).Take(10).ToList();
        }

        public void HandOut()
        {
            foreach (var user in _usersManager.GetOnlineUsers().Where(user => user.Coins < MaxAutoCoins))
            {
                AddCoinsToUser(user.Name, HandOutAmount);
            }
        }

        public void AddCoinsToAllOnline(int coins)
        {
            _usersManager.GetOnlineUsers().ForEach(viewer => { viewer.Coins += coins; _databaseManager.UpdateViewer(viewer); });
            Log("Added " + coins + " " + CurrencyName + " to everyone without message.");
        }

        public void AddCoinsToUser(string username, int coins)
        {
            var userId = _usersManager.GetUserId(username);
            AddCoinsToUser(userId, coins);
        }

        public void AddCoinsToUser(int userId, int coins)
        {
            var viewer = _usersManager.GetUserById(userId);
            viewer.Coins += coins;
            _databaseManager.UpdateViewer(viewer);
        }

        public void RemoveCoinsFromUser(string username, int coins)
        {
            AddCoinsToUser(username, -coins);
        }

        public void RemoveCoinsFromAllOnline(int coins)
        {
            AddCoinsToAllOnline(-coins);
        }

        public int GetUserCoins(string username)
        {
            return _usersManager.GetUserByName(username).Coins;
        }

        public int GetUserCoins(int userId)
        {
            return _usersManager.GetUserById(userId).Coins;
        }
    }
}
