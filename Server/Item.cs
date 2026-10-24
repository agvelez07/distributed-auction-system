namespace Server
{
    public class Item
    {
        public string Name { get; }
        public int StartingPrice { get; }

        public Item(string name, int startingPrice)
        {
            Name = name;
            StartingPrice = startingPrice;
        }
    }
}
