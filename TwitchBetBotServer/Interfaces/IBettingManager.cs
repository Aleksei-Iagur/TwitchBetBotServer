using System.Collections.Generic;
using PrismataTvServer.Enums;

namespace PrismataTvServer.Interfaces
{
    public interface IBettingManager
    {
        bool BettingOpen { get; set; }
        bool PoolLocked { get; set; }
        void OpenBetting();
        void CloseBetting();
        void CancelBetting();
        void SetWinner(GameResult gameResult);
        void ShowCurrentBets();
        void ShowHelp();
        void PlaceBet(string username, int amount, GameResult option);
        void PlaceBet(string username, int amount, int option);
        void ShowBetForUser(string username);
        bool IsUserInPool(string username);
        int GetUserBetAmount(string username);
        List<string> GetCurrentBets();
        void DeletePool();
        void CreateNewGame();
        void UpdateGamePlayers(string player1, string player2);
        int GetMinBetForUser(string username);
        int GetMinBetForUser(int userId);
        string GetUserBetOn(string username);
    }
}
