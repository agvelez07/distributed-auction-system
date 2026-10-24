namespace Shared
{
    // Shared by the server and the client so both use the same messages
    public static class Protocol
    {
        public const int Port = 6065;
    }

    // Client -> Server
    // Name <name> | Bid <amount> | Status | Exit
    public enum ClientCommand
    {
        Name,
        Bid,
        Status,
        Exit
    }

    // Server -> Client
    // NameOk | NameTaken | EntryClosed
    // Start <price> <duration> <item>
    // Accepted <amount> | Rejected <highest>
    // Status <amount> <bidder> | End <amount> <winner>   (0 = no bids / not sold)
    // Bye | Error
    public enum ServerResponse
    {
        NameOk,
        NameTaken,
        EntryClosed,
        Start,
        Accepted,
        Rejected,
        Status,
        Bye,
        End,
        Error
    }
}
