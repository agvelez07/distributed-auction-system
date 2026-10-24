using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Shared;


namespace Server
{
    public class AuctionServer
    {
        private readonly Auction auction;
        private readonly int entrySec;
        private readonly List<ClientHandler> handlers = new List<ClientHandler>();

        public AuctionServer(Auction auction, int entrySec)
        {
            this.auction = auction;
            this.entrySec = entrySec;
        }

        private void Log(string message)
        {
            Console.WriteLine($"Thread: [{Thread.CurrentThread.Name}] - Message: {message}");
        }

        public void Run()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, Protocol.Port);

            try
            {
                listener.Start();
                Log($"Listening on port {Protocol.Port}. Entry open for {entrySec}s");

                AcceptClients(listener);
                listener.Stop();

                int count = auction.CloseEntry();
                Log($"Entry closed with {count} participant(s)");

                Log($"AUCTION STARTED: {auction.Item.Name}, starting price {auction.Item.StartingPrice}, duration {auction.DurationSec}s");
                auction.Start(count);

                Thread.Sleep(auction.DurationSec * 1000);

                auction.End();
                PrintResult();

                foreach (ClientHandler handler in handlers)
                {
                    handler.Join();
                }

                Log("Server Finished");
            }
            catch (SocketException e)
            {
                Log($"Network error: {e.Message}");

            }
            finally
            {
                listener.Stop();
            }
        }

        private void AcceptClients(TcpListener listener)
        {
            Stopwatch clock = Stopwatch.StartNew();
            int id = 1;

            while (clock.Elapsed.TotalSeconds < entrySec)
            {
                if (listener.Pending())
                {
                    TcpClient socket = listener.AcceptTcpClient();
                    ClientHandler handler = new ClientHandler(socket, auction, id);
                    handlers.Add(handler);
                    handler.Start();
                    id++;
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void PrintResult()
        {
            Bid? highest = auction.GetHighestBid();

            Log("===== AUCTION ENDED =====");

            if (highest == null)
            {
                Log($"'{auction.Item.Name}' was not sold");
            }
            else
            {
                Log($"Winner: {highest.Bidder} with {highest.Amount}");
            }

            Log("Waiting for active clients to receive the result...");
        }

    }
}
