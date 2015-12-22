using System;
using PrismataTvServer.Enums;

namespace PrismataTvServer.Classes
{
    public class GameInViewer
    {
        public int GameId { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public DateTime Date { get; set; }
        public GameResult Result { get; set; }
        public int BetsOnP1Count { get; set; }
        public int BetsOnP2Count { get; set; }
        public int BetsOnDrawCount { get; set; }
        public int BetsOnP1Amount { get; set; }
        public int BetsOnP2Amount { get; set; }
        public int BetsOnDrawAmount { get; set; }
        public bool IsLocked { get; set; }
    }
}
