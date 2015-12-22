using System;
using System.Collections.Generic;
using PrismataTvServer.Classes;

namespace PrismataTvServer.Interfaces
{
    public interface IDatabaseManager
    {
        int CreateNewUser(string user);
        void UpdateOrInsertViewers(IEnumerable<User> viewers);
        IEnumerable<User> GetAllUsers();
        void SaveGame(Game game);
        List<Player> GetAllPlayers();
        void SavePlayer(Player player);
        void SaveOption(UserOption option);
        void UpdateViewer(User viewer);
        Dictionary<int, List<UserOption>> GetAllViewersOptions();
        IEnumerable<GameInViewer> GetGamesOnDate(DateTime date);
        void ChangeUserPassword(User user);
    }
}
