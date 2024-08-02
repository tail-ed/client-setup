# CLIENT GRPC (C#) FOR TIC-TAC-TOE GAME

## What is this?
this program contains the csharp client to play tictactoe using grpc including the proto file, the grpc and csharp files and the program.cs which contains a basic tictactoe bot and all the logic to start and play a match.

## How to Play?
- The **`Program.cs`** file facilitates establishing connections, sending requests, and receiving responses from the server.
- Once connected, you will start by sending a **ConnectRequest** and later receive  **Events** and **PlayerTurnRequests**:
    - to ensure you are properly able to play make sure your client-id ex: progam.cs line 19 is filled out with your UUID.
    - **ConnectRequest:** this is to connect you to your webgl build ensure your ClientId is filled with your username.
    - **Event:** Contains game information such as whose turn it is, command success, and failures.
    - **PlayerTurnRequests:** Indicates that it is your turn to play, and the server is waiting for your input. An Array is included to inform you of how the board is occupied.

## How to Send Input?
- Input is sent using the **PutTokensAsync(TTTGameService.TTTGameServiceClient client)** function that in turn uses **client.PutTokenAsync(putTokenRequest)** to send the puttokenrequest to the server. To discover available methods, invoke a help request in the **`Main()`**. The server will return descriptions of how the functions work.

## Where to Begin?
- in **HandlePlayerTurnResponseAsync(IAsyncStreamReader<PlayerTurnResponse> responseStream, TTTGameService.TTTGameServiceClient client)** you can find the logic for populating the board variable. and then use the **PutTokensAsync(TTTGameService.TTTGameServiceClient client)** to dicide where to place your token.

## How to Connect?
- Initiate a connection using the **`ConnectToServerAsync()`** function in **`Main()`**.
    - **Singleplayer** : use "localhost" to play against a bot, which allows you to test your script in a controlled environment.
    - **Multiplayer**  : Enter the server address provided on the Tail'ed website for PvP matchmaking (not yet implemented).

## How the Structure Works:
- Upon launch, **`Program.cs`** connects to the server using **client.Connect(connectReq)** in **`Main()`**.
- It also starts an asynchronous two tasks:**client.PlayerTurn(playerTurnRequest)** and **client.Event(emptyRequest)** which listens to server responses.
- Received responses are handled by **HandleEventResponseAsync** and **HandlePlayerTurnResponseAsync**, where you decide how to react based on whether it's an "Event" or an "Action".
- Actions determined by your code are sent using **PutTokensAsync()**, which uses grpc to send your action.
Feel free to adapt the system to your preferences if you find a more efficient method.

## MAKE SURE TO INSTALL .NET SDK 8.0!