using PrismataTvServer.Classes;

namespace PrismataTvServer.Interfaces
{
    public interface IPlayersManager
    {
        void AddPlayer(string playerName);
        int GetPlayerId(string playerName);
        Player GetPlayer(int playerId);
    }
}
