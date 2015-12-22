namespace PrismataTvServer.Classes
{
    public class PoolUser
    {
        public int BetOn { get; set; }
        public int BetAmount { get; set; }

        public PoolUser(int betOn, int betAmount)
        {
            BetOn = betOn;
            BetAmount = betAmount;
        }
    }
}
