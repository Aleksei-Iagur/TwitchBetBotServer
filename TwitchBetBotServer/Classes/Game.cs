using System;
using System.Collections.Generic;
using PrismataTvServer.Enums;

namespace PrismataTvServer.Classes
{
    public class Game
    {
        public int GameId { get; set; }
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public GameResult Result { get; set; }
        public DateTime Date { get; set; }
        public List<Bet> Bets { get; set; }

        public Game() { }

        public Game(int gameId, int player1Id, int player2Id, GameResult result, DateTime date, IEnumerable<Bet> bets)
        {
            GameId = gameId;
            Player1Id = player1Id;
            Player2Id = player2Id;
            Result = result;
            Date = date;
            Bets = new List<Bet>(bets);
        }

        public string GetDateAsString()
        {
            return Date.ToString("yyyy-MM-dd HH:mm:ss"); 
        }
    }
}
