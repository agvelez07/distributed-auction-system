namespace Server
{
    public class Bid
    {
        public string Bidder { get;  }
        public int Amount { get; }
    
        public Bid(string bidder, int amount)
        {
            Bidder = bidder;
            Amount = amount;
        }
    }
}
