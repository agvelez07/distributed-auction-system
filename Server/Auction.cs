using Shared;

namespace Server
{
    // Shared auction state. All access to it is protected by the mutex.
    public class Auction
    {
        public Item Item { get; }
        public int DurationSec { get; }

        // Only one thread at a time can read/change the state below
        private readonly Mutex mutex = new Mutex();

        // Starts at 0. Client threads block here until the auction starts
        private readonly SemaphoreSlim startSemaphore = new SemaphoreSlim(0);

        private readonly List<string> participants = new List<string>();
        private bool entryOpen = true;
        private bool ended = false;
        private Bid? highestBid = null;

        public Auction(Item item, int durationSec)
        {
            Item = item;
            DurationSec = durationSec;
        }

        // Check and add in the same critical section
        // names stay unique
        public ServerResponse AddParticipant(string name)
        {
            mutex.WaitOne();
            try
            {
                if (!entryOpen)
                {
                    return ServerResponse.EntryClosed;
                }

                if (participants.Contains(name))
                {
                    return ServerResponse.NameTaken;
                }

                participants.Add(name);

                return ServerResponse.NameOk;
            }

            finally
            {
                mutex.ReleaseMutex();
            }
        }


        public void RemoveParticipant(string name)
        {
            mutex.WaitOne();
            try
            {
                participants.Remove(name);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }


        // No more participants after this.
        // Returns how many joined.
        public int CloseEntry()
        {
            mutex.WaitOne();
            try
            {
                entryOpen = false;
                return participants.Count;

            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        // Called by the server
        // one permit per participant, releases all waiting threads
        public void Start(int participantCount)
        {
            if (participantCount > 0)
            {
                startSemaphore.Release(participantCount);
            }
        }

        // Called by each client thread. blocks until Start()
        public void WaitForStart()
        {
            startSemaphore.Wait();
        }

        // Validate and update in ONE critical section (atomic):
        // two threads can never accept a bid based on the same previous state
        public bool TryPlaceBid(Bid bid, out Bid? highest)
        {
            mutex.WaitOne();
            try
            {
                bool valid;

                if (ended)
                {
                    valid = false;
                }
                else if (highestBid == null)
                {
                    // First bid (must reach the starting price)
                    valid = bid.Amount >= Item.StartingPrice;
                }
                else
                {
                    // Bid already exists and must beat the current highest
                    valid = bid.Amount > highestBid.Amount;
                }

                if (valid)
                {
                    highestBid = bid;
                }

                highest = highestBid;
                return valid;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public Bid? GetHighestBid()
        {
            mutex.WaitOne();
            try
            {
                return highestBid;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        // Called by the server when the time's up
        public void End()
        {
            mutex.WaitOne();
            try
            {
                ended = true;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public bool HasEnded()
        {
            mutex.WaitOne();
            try
            {
                return ended;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}