namespace Server
{
    public class Item
    {
        public string Name { get; }
        public decimal StartingPrice { get; }

        public Item(string name, decimal startingPrice)
        {
            Name = name;
            StartingPrice = startingPrice;
        }
    }
}
