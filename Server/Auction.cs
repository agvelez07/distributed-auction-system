namespace Server
{
    public class Auction
    {
        public Item item { get; }
        public int DurationSec { get; }

        private readonly Mutex mutex = new Mutex();
        private readonly SemaphoreSlim startSemaphore = new SemaphoreSlim(0);

        private readonly List<string> participans

    } 

}
