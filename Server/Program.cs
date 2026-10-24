namespace Server
{
    public class Program
    {
        // Seconds the server accepts new clients
        private const int EntrySec = 120;

        public static void Main()
        {
            Thread.CurrentThread.Name = "Main";

            // Ask for the auction data
            string itemName = "";
            while (itemName == "")
            {
                Console.Write("Item Name: ");
                itemName = (Console.ReadLine() ?? "").Trim();
            }

            int startingPrice = ReadPositiveInt("Starting price: ");
            int duration = ReadPositiveInt("Auction duration (seconds): ");

            Item item = new Item(itemName, startingPrice);
            Auction auction = new Auction(item, duration);

            new AuctionServer(auction, EntrySec).Run();
        }

        // Asks until the user writes an integer > 0
        private static int ReadPositiveInt(string text)
        {
            while (true)
            {
                Console.Write(text);
                if (int.TryParse(Console.ReadLine(), out int value) && value > 0)
                {
                    return value;
                }
                Console.WriteLine("Please enter an integer greater than 0");
            }
        }
    }
}