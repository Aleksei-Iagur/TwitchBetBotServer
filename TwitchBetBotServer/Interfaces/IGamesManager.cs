using System.Collections.Generic;
using PrismataTvServer.Classes;
using PrismataTvServer.Enums;

namespace PrismataTvServer.Interfaces
{
    public interface IGamesManager
    {
        void CreateNewGame();
        void CloseGame(GameResult gameResult);
        void ChangeGameResult(GameResult newResult, int gameId);
        void PlaceBet(int userId, GameResult option, int amount);
        int GetTotalBetsForClosedGame();
        int GetTotalBetsForCurrentGame();
        List<Winner> GetWinners();
        void CancelCurrentGame();
        int GetTotalBetsOn(GameResult gameResult);
        int GetNumberOfBets(GameResult gameResult);
        bool UserHasBet(int userId);
        int GetBetAmount(int userId);
        string GetBetOn(int userId);
        string GetOptionName(int option);
        void UpdateGamePlayers(string player1, string player2);
        List<Game> GetAllGames();
        void SaveGame();
    }
}
