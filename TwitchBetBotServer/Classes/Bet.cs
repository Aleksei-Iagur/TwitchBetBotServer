using PrismataTvServer.Enums;

namespace PrismataTvServer.Classes
{
    public class Bet
    {
        public int GameId { get; set; }
        public int ViewerId { get; set; }
        public GameResult BetOn { get; set; }
        public int BetAmount { get; set; }
        public int WinAmount { get; set; }
        public bool IsWin { get; set; }
    }
}
