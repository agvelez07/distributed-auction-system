namespace Server { 
    public static class Protocol
    {
        public const int Port = 6065;
    }

    public enum ClientCommand {
        Name,
        Bid,
        Status,
        Exit
    }

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