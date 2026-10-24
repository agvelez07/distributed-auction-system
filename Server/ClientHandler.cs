using System.Net.Sockets;
using Shared;

namespace Server
{
    // One thread per connected client
    public class ClientHandler
    {
        // A client that sends no valid name in this time is dropped
        private const int NameTimeoutMs = 30000;

        private readonly TcpClient socket;
        private readonly Auction auction;
        private readonly Thread thread;
        private string? name; // set when the name is accepted

        public ClientHandler(TcpClient socket, Auction auction, int id)
        {
            this.socket = socket;
            this.auction = auction;
            thread = new Thread(Run);
            thread.Name = "ClientThread-" + id;
        }

        public void Start()
        {
            thread.Start();
        }
        public void Join()
        {
            thread.Join();
        }

        // Shows which thread handled each message
        private void Log(string message)
        {
            Console.WriteLine($"Thread: [{Thread.CurrentThread.Name}] - Message: {message}");
        }

        private void Run()
        {
            try
            {
                NetworkStream stream = socket.GetStream();
                StreamReader reader = new StreamReader(stream);
                StreamWriter writer = new StreamWriter(stream)
                {
                    AutoFlush = true
                };

                Log("Client Connected");

                // Don't wait forever for a client that never sends a name
                socket.ReceiveTimeout = NameTimeoutMs;
                if (!Register(reader, writer))
                {
                    return;
                }
                socket.ReceiveTimeout = 0; // no timeout during the auction

                // Blocks here until the server starts the auction
                Log($"{name} is waiting for the auction to start");
                auction.WaitForStart();

                // Start <price> <duration> <item name>
                writer.WriteLine($"{ServerResponse.Start} {auction.Item.StartingPrice} {auction.DurationSec} {auction.Item.Name}");

                HandleRequests(reader, writer);

            }
            catch (IOException)
            {
                Log("Connection Lost");
            }
            finally
            {
                // Leaving does not cancel the bid, only the name is removed
                if (name != null)
                {
                    auction.RemoveParticipant(name);
                }

                socket.Close();
                Log(name == null ? "Finished (no name)" : $"Finished ({name})");

            }

        }

        // Returns true when the name is accepted
        private bool Register(StreamReader reader, StreamWriter writer)
        {
            while (true)
            {
                string? message = reader.ReadLine();
                if (message == null)
                {
                    return false;
                }

                // Expect's Name <name>
                string[] parts = message.Split(' ', 2);

                if (parts.Length < 2 || parts[0] != ClientCommand.Name.ToString() || parts[1].Trim() == "")
                {
                    writer.WriteLine(ServerResponse.Error);
                    continue;
                }

                string candidate = parts[1].Trim();
                ServerResponse response = auction.AddParticipant(candidate);
                writer.WriteLine(response);

                if (response == ServerResponse.NameOk)
                {
                    name = candidate;
                    Log($"'{name}' joined the auction!");
                    return true;
                }
                if (response == ServerResponse.EntryClosed)
                {
                    Log("Client Refused: Entry Period is Over");
                    return false;
                }

                Log($"Name '{candidate}' is already taken");

            }
        }
        private void HandleRequests(StreamReader reader, StreamWriter writer)
        {
            while (true)
            {
                string? message = reader.ReadLine();
                if (message == null)
                {
                    Log($"{name} disconnected");
                    return;
                }
                // After the end. Result is the reply to the next message
                if (auction.HasEnded())
                {
                    SendResult(writer);
                    return;
                }
                string[] parts = message.Split(' ');
                if (!Enum.TryParse(parts[0], out ClientCommand command))
                {
                    writer.WriteLine(ServerResponse.Error);
                    continue;
                }

                switch (command)
                {
                    case ClientCommand.Bid:
                        if (parts.Length < 2 || !int.TryParse(parts[1], out int amount))
                        {
                            writer.WriteLine(ServerResponse.Error);
                            break;

                        }
                        if (HandleBid(amount, writer))
                        {
                            return; // auction already ended
                        }
                        break;
                    case ClientCommand.Status:
                        SendStatus(writer);
                        break;
                    case ClientCommand.Exit:
                        Log($"{name} left the auction");
                        writer.WriteLine(ServerResponse.Bye);
                        return;
                    default:
                        writer.WriteLine(ServerResponse.Error);
                        break;
                }
            }
        }

        // Returns true if the auction has ended
        private bool HandleBid(int amount, StreamWriter writer)
        {
            Log($"{name} bid {amount}");

            bool accepted = auction.TryPlaceBid(new Bid(name!, amount), out Bid? highest);

            if (accepted)
            {
                Log($"bid of {name} ({amount}) ACCEPTED");
                writer.WriteLine($"{ServerResponse.Accepted} {amount}");
                return false;
            }

            // Rejected because time is up
            if (auction.HasEnded())
            {
                SendResult(writer);
                return true;
            }

            // Rejected because it was too low (0 = no bids yet)
            int current = highest == null ? 0 : highest.Amount;
            Log($"bid of {name} ({amount}) REJECTED, highest is {current}");
            writer.WriteLine($"{ServerResponse.Rejected} {current}");
            return false;
        }

        // Status <amount> <bidder>   or   Status 0 (no bids yet)
        private void SendStatus(StreamWriter writer)
        {
            Bid? highest = auction.GetHighestBid();

            if (highest == null)
            {
                writer.WriteLine($"{ServerResponse.Status} 0");
            }
            else
            {
                writer.WriteLine($"{ServerResponse.Status} {highest.Amount} {highest.Bidder}");
            }
        }

        // End <amount> <winner>   or   End 0 (not sold)
        private void SendResult(StreamWriter writer)
        {
            Bid? highest = auction.GetHighestBid();

            if (highest == null)
            {
                writer.WriteLine($"{ServerResponse.End} 0");
            }
            else
            {
                writer.WriteLine($"{ServerResponse.End} {highest.Amount} {highest.Bidder}");
            }

            Log($"Result sent to {name}");
        }
    }
}