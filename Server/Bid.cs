namespace Server
{
    public class Bid
    {
        public string Bidder { get;  }
        public decimal Amount { get; }
    
        public Bid(string bidder, decimal amount)
        {
            Bidder = bidder;
            Amount = amount;
        }
    }
}
