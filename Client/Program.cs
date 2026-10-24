using System.Net.Sockets;
using Shared;

namespace Client
{
    public class Program
    {
        private static int startingPrice;

        public static void Main()
        {
            Console.Write("Server IP (Enter = localhost): ");
            string host = (Console.ReadLine() ?? "").Trim();
            if (host == "")
            {
                host = "127.0.0.1";
            }

            try
            {
                using TcpClient socket = new TcpClient(host, Protocol.Port);
                NetworkStream stream = socket.GetStream();
                StreamReader reader = new StreamReader(stream);
                StreamWriter writer = new StreamWriter(stream)
                {
                    AutoFlush = true
                };

                Console.WriteLine($"Connected to {host}:{Protocol.Port}");

                if (!Register(reader, writer))
                {
                    return;
                }

                if (!WaitForStart(reader))
                {
                    return;
                }

                HandleInput(reader, writer);
            }
            catch (SocketException)
            {
                // The server closes the port when the entry period is over
                Console.WriteLine("Could not connect. The server is off or the entry period is over.");
            }
            catch (IOException)
            {
                Console.WriteLine("Connection to the server was lost.");
            }

            Console.WriteLine("Press Enter to close...");
            Console.ReadLine();
        }

        // Returns true when the name is accepted
        private static bool Register(StreamReader reader, StreamWriter writer)
        {
            while (true)
            {
                Console.Write("Your name: ");
                string? line = Console.ReadLine();
                if (line == null)
                {
                    return false; // input closed
                }

                string name = line.Trim();
                if (name == "")
                {
                    continue;
                }

                writer.WriteLine($"{ClientCommand.Name} {name}");
                string? message = reader.ReadLine();
                if (message == null)
                {
                    Console.WriteLine("Server closed the connection.");
                    return false;
                }

                if (!Enum.TryParse(message, out ServerResponse response))
                {
                    response = ServerResponse.Error;
                }

                switch (response)
                {
                    case ServerResponse.NameOk:
                        Console.WriteLine($"Welcome {name}! Waiting for the auction to start...");
                        return true;
                    case ServerResponse.NameTaken:
                        Console.WriteLine("That name is already taken. Try another one.");
                        break;
                    case ServerResponse.EntryClosed:
                        Console.WriteLine("The entry period is over. You can't join this auction.");
                        return false;
                    default:
                        Console.WriteLine("Invalid name. Try again.");
                        break;
                }
            }
        }

        // Blocks until the server sends: Start <price> <duration> <item>
        private static bool WaitForStart(StreamReader reader)
        {
            string? message = reader.ReadLine();
            if (message == null)
            {
                Console.WriteLine("Server closed the connection.");
                return false;
            }

            string[] parts = message.Split(' ', 4);
            if (parts.Length < 4 || parts[0] != ServerResponse.Start.ToString())
            {
                Console.WriteLine($"Unexpected message: {message}");
                return false;
            }

            startingPrice = int.Parse(parts[1]);

            Console.WriteLine();
            Console.WriteLine("===== AUCTION STARTED =====");
            Console.WriteLine($"Item: {parts[3]}");
            Console.WriteLine($"Starting price: {parts[1]}");
            Console.WriteLine($"Duration: {parts[2]}s");
            Console.WriteLine("Commands: <amount> = bid | status | sair");
            return true;
        }

        private static void HandleInput(StreamReader reader, StreamWriter writer)
        {
            while (true)
            {
                Console.Write("> ");
                string input = (Console.ReadLine() ?? "sair").Trim().ToLower();

                string request;
                if (input == "sair" || input == "exit")
                {
                    request = ClientCommand.Exit.ToString();
                }
                else if (input == "status")
                {
                    request = ClientCommand.Status.ToString();
                }
                else if (int.TryParse(input, out int amount) && amount > 0)
                {
                    request = $"{ClientCommand.Bid} {amount}";
                }
                else
                {
                    Console.WriteLine("Unknown command. Use: <amount> | status | sair");
                    continue;
                }

                writer.WriteLine(request);
                string? message = reader.ReadLine();
                if (message == null)
                {
                    Console.WriteLine("Server closed the connection.");
                    return;
                }

                // Returns true when the client must stop
                if (ShowResponse(message))
                {
                    return;
                }
            }
        }

        private static bool ShowResponse(string message)
        {
            string[] parts = message.Split(' ', 3);
            if (!Enum.TryParse(parts[0], out ServerResponse response))
            {
                response = ServerResponse.Error;
            }

            int amount = 0;
            if (parts.Length > 1)
            {
                int.TryParse(parts[1], out amount);
            }

            switch (response)
            {
                case ServerResponse.Accepted:
                    Console.WriteLine($"Bid ACCEPTED. Highest bid is now {amount}");
                    return false;

                case ServerResponse.Rejected:
                    if (amount == 0)
                    {
                        Console.WriteLine($"Bid REJECTED. No bids yet, the minimum is {startingPrice}");
                    }
                    else
                    {
                        Console.WriteLine($"Bid REJECTED. Highest bid is {amount}");
                    }
                    return false;

                case ServerResponse.Status:
                    if (amount == 0)
                    {
                        Console.WriteLine($"No bids yet. Starting price is {startingPrice}");
                    }
                    else
                    {
                        Console.WriteLine($"Highest bid: {amount} by {parts[2]}");
                    }
                    return false;

                case ServerResponse.Bye:
                    Console.WriteLine("You left the auction.");
                    return true;

                case ServerResponse.End:
                    Console.WriteLine();
                    Console.WriteLine("===== AUCTION ENDED =====");
                    if (amount == 0)
                    {
                        Console.WriteLine("The item was not sold.");
                    }
                    else
                    {
                        Console.WriteLine($"Winner: {parts[2]} with {amount}");
                    }
                    return true;

                default:
                    Console.WriteLine($"Server error: {message}");
                    return false;
            }
        }
    }
}
