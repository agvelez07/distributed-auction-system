# Distributed Auction System

A client/server auction written in **C# (.NET 10)** for the *Distributed Systems* course (Politécnico de Portalegre, 2026/2027).

The server offers one item. Several clients connect over **TCP sockets** and bid on it until time runs out. The server is **multi-threaded**, with one thread per client. Shared state is protected **only with `Mutex` and semaphores**; no `lock` is used.

---

## Features

- TCP communication on port **6065**
- One server thread per client (`ClientThread-N`), shown in every log line
- Entry period of **30 s**. After it ends, no new participants are accepted
- Usernames must be unique
- Client threads wait for the auction to start before they accept bids
- **Atomic bid validation and update**: two concurrent bids can never both be accepted on the same previous state
- The first bid must be **≥ starting price**. Later bids must be **> current highest**
- Clients can check the highest bid at any time (`status`) or leave (`sair`)
- If the highest bidder leaves, their bid still stands
- When time runs out, each active client receives the result as the reply to its next message
- Exceptions are handled so that neither side crashes (lost connections, server off, invalid input)

---

## Project structure

```
distributed-auction-system/
├── distributed_auction_system.slnx
├── Shared/
│   └── Protocol.cs          # Port + ClientCommand / ServerResponse enums
├── Server/
│   ├── Program.cs           # Reads item, starting price and duration
│   ├── AuctionServer.cs     # Entry period, start, timer, end, Join threads
│   ├── ClientHandler.cs     # One thread per client
│   ├── Auction.cs           # Shared state (Mutex + SemaphoreSlim)
│   ├── Item.cs              # Name, StartingPrice (immutable)
│   └── Bid.cs               # Bidder, Amount (immutable)
└── Client/
    └── Program.cs           # Console client
```

---

## Protocol

Text messages, one per line. Values are integers. `0` means *no bids* or *not sold*.

### Client → Server

| Message        | Meaning               |
|----------------|-----------------------|
| `Name <name>`  | Register the username |
| `Bid <amount>` | Place a bid           |
| `Status`       | Ask for the highest bid |
| `Exit`         | Leave the auction     |

### Server → Client

| Message                            | Meaning                                   |
|------------------------------------|-------------------------------------------|
| `NameOk` / `NameTaken`             | Name accepted / already in use            |
| `EntryClosed`                      | Entry period is over                      |
| `Start <price> <duration> <item>`  | Auction started                           |
| `Accepted <amount>`                | Bid accepted (new highest bid)            |
| `Rejected <highest>`               | Bid rejected, with the current highest    |
| `Status <amount> <bidder>`         | Current highest bid (`Status 0` = none)   |
| `End <amount> <winner>`            | Auction ended (`End 0` = not sold)        |
| `Bye`                              | Exit confirmed                            |
| `Error`                            | Invalid message                           |

### Example

```
C: Name Ana            S: NameOk
                       S: Start 100 60 Laptop
C: Bid 50              S: Rejected 0
C: Bid 120             S: Accepted 120
C: Status              S: Status 120 Ana
...                    (time is up)
C: Status              S: End 120 Ana
```

---

## Synchronization

| Mechanism                     | Where                 | Why |
|-------------------------------|-----------------------|-----|
| `Mutex`                       | `Auction`             | Protects `participants`, `entryOpen`, `ended` and `highestBid`. `TryPlaceBid` validates **and** updates in one critical section, which makes it atomic. |
| `SemaphoreSlim(0)`            | `Auction`             | Client threads block in `WaitForStart()`. `Start(count)` calls `Release(count)` and frees all of them at once. |
| `AddParticipant` (inside Mutex) | `Auction`           | Checks the name and adds it in the same critical section, so names stay unique. |

---

## How to run

Requires the **.NET 10 SDK**.

```powershell
# 1. Build once
dotnet build

# 2. Start the server (terminal 1)
dotnet run --project Server --no-build
#   Item Name: Laptop
#   Starting price: 100
#   Auction duration (seconds): 60

# 3. Start clients within 30 s (terminals 2, 3, 4, ...)
dotnet run --project Client --no-build
#   Server IP: <Enter> for localhost, or the server's IPv4 address
#   Your name: Ana
```

### Client commands

| Command    | Action                 |
|------------|------------------------|
| `<number>` | Bid that amount        |
| `status`   | Show the highest bid   |
| `sair`     | Leave the auction      |

> To run on different computers, use the server's IPv4 address (`ipconfig`) and allow port 6065 in the Windows Firewall.

---

## Tested scenarios

- [x] Duplicate username → `NameTaken`, then a new name is accepted
- [x] Client connecting after the entry period → connection refused
- [x] Client that never sends a name → dropped after 30 s
- [x] Bid below the starting price → rejected
- [x] Bid equal to the starting price → accepted
- [x] Bid not higher than the current highest → rejected
- [x] ~200 concurrent bids from two clients → no duplicates, consistent state
- [x] Highest bidder leaves with `sair` → bid still stands
- [x] Client closed abruptly → server logs it and continues
- [x] Auction ends with a winner / with no bids (not sold)
- [x] Server stopped → client shows "connection lost" without crashing

---

## Author

- António Velez — 23406
