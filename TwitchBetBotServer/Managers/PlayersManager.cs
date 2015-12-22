using System;
using System.Collections.Generic;
using System.Linq;
using PrismataTvServer.Classes;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Managers
{
    public class PlayersManager : IPlayersManager
    {
        private readonly List<Player> _players;
        private readonly IDatabaseManager _databaseManager;

        public PlayersManager(List<Player> initList, IDatabaseManager databaseManager)
        {
            _players = initList;
            _databaseManager = databaseManager;
        }

        public void AddPlayer(string playerName)
        {
            var player = new Player {Id = GetNewId(), Name = playerName, RegistrationDate = DateTime.Now};
            _players.Add(player);
            _databaseManager.SavePlayer(player);
        }

        private int GetNewId()
        {
            return _players.Any() ? _players.Max(x => x.Id) + 1 : 1;
        }

        public int GetPlayerId(string playerName)
        {
            if (!_players.Any(x => x.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
            {
                AddPlayer(playerName);
            }

            return _players.Find(x => x.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)).Id;
        }

        public Player GetPlayer(int playerId)
        {
            return _players.Find(x => x.Id == playerId);
        }
    }
}
