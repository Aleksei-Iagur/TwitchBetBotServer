using System.Collections.Generic;
using System.Threading;
using PrismataTvServer.Classes;

namespace PrismataTvServer.Interfaces
{
    public interface ICurrencyManager
    {
        void AddToLookups(string username, IList<string> usersToLookup, Timer currencyQueueTimer);
        void ShowTop10();
        List<User> GetTop10();
        void CheckUserCurrency(string username);
        void AddCoinsToAllOnline(int coins);
        void AddCoinsToAllWithMessage(int amount);
        void AddCoinsToUser(int userId, int coins);
        void AddCoinsToUser(string username, int coins);
        void AddCoinsToUserWithMessage(string username, int amount);
        void RemoveCoinsFromAllOnline(int coins);
        void RemoveCurrencyFromAll(int amount, string operatorUser);
        void RemoveCoinsFromUser(int userId, int coins);
        void RemoveCoinsFromUser(string username, int coins);
        void RemoveCurrencyFromUser(string username, int amount, string operatorUser);
        void HandOut();
        int GetUserCoins(string username);
        int GetUserCoins(int userId);
        string CurrencyName { get; }
    }
}
