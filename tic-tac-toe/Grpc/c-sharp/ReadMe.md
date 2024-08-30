# CLIENT gRPC (C#) FOR TIC-TAC-TOE GAME

## What is this?
This program contains the C# client to play TicTacToe using gRPC, including the proto file, the gRPC and C# files, and the `Program.cs`, which contains a basic TicTacToe bot and all the logic to start and play a match.

## How to Play?
- The **`Program.cs`** file facilitates establishing connections, sending requests, and receiving responses from the server.
- Once connected, you will start by sending a **ConnectRequest** and later receive **Events** and **PlayerTurnRequests**:
  - To ensure you are properly able to play, make sure your client-id (e.g., `Program.cs` line 19) is filled out with your UUID.
  - **ConnectRequest:** This is to connect you to your WebGL build. Ensure your ClientId is filled with your username.
  - **Event:** Contains game information such as whose turn it is, command success, and failures.
  - **PlayerTurnRequests:** Indicates that it is your turn to play, and the server is waiting for your input. An array is included to inform you of how the board is occupied.

## How to Send Input?
- Input is sent using the **PutTokensAsync(TTTGameService.TTTGameServiceClient client)** function that in turn uses **client.PutTokenAsync(putTokenRequest)** to send the PutTokenRequest to the server. To discover available methods, invoke a help request in the **`Main()`**. The server will return descriptions of how the functions work.

## Where to Begin?
- In **HandlePlayerTurnResponseAsync(IAsyncStreamReader<PlayerTurnResponse> responseStream, TTTGameService.TTTGameServiceClient client)**, you can find the logic for populating the board variable. Then use **PutTokensAsync(TTTGameService.TTTGameServiceClient client)** to decide where to place your token.

## How to Connect?
- Initiate a connection using the **`ConnectToServerAsync()`** function in **`Main()`**.
  - **Singleplayer**: Use "localhost" to play against a bot, which allows you to test your script in a controlled environment.
  - **Multiplayer**: Enter the server address provided on the Tail'ed website for PvP matchmaking (not yet implemented).

## How the Structure Works:
- Upon launch, **`Program.cs`** connects to the server using **client.Connect(connectReq)** in **`Main()`**.
- It also starts two asynchronous tasks: **client.PlayerTurn(playerTurnRequest)** and **client.Event(emptyRequest)**, which listen to server responses.
- Received responses are handled by **HandleEventResponseAsync** and **HandlePlayerTurnResponseAsync**, where you decide how to react based on whether it's an "Event" or an "Action".
- Actions determined by your code are sent using **PutTokensAsync()**, which uses gRPC to send your action. Feel free to adapt the system to your preferences if you find a more efficient method.

## Make sure to install .NET SDK 8.0!