using System.Net.Sockets;
using System.Xml.Serialization;

namespace Server
{
    public class ClientHandler
    {
        public class ClientHandle
        {
            private readonly TcpClient socket;
            private readonly Auction auction;
            private readonly Thread thread;
            private string? name;

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

            private void Log(string message)
            {
                Console.WriteLine($"Thread: [{Thread.CurrentThread.Name}] - Message: {message}");
            }

            private void Run()
            {
                try
                {
                    NetworkStream stream = socket.GetStream();
                    StreamReader reade = new StreamReader(stream);
                    StreamWriter write = new StreamWriter(stream)
                    {
                        AutoFlush = true
                    };

                    Log("Client Connected");

                    if (!Register(reader, writer))
                    {
                        return;
                    }

                    Log($"{name} is waiting for the auction to start");
                    auction.waitForStart();

                    writer.WriteLine($"{ServerResponse.Start} {auction.item.StartingPrice} {auction.DurationSec} {auction.item.Name}");

                    HandleRequests(reader, write);

                }
                catch (IOException)
                {
                    Log("Connection Lost");
                }
                finally
                {
                    if (name != null){
                        auction.RemoveParticipant(nanme)M
                    }

                    socket.Close();
                    Log($"Finished ({name})");

                }

            }

        }
    }
}
