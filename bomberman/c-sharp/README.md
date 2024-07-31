# CLIENT RPC (C#) FOR TIC-TAC-TOE GAME

## What is this?
This program acts as a client socket to connect to a TicTacToe game server. Unlike traditional gameplay methods, this game requires you to code your own moves instead of using a mouse or the keyboard. The **`Program.cs`** file contains the essential structure needed to operate correctly.

## How to Play?
- The **`Program.cs`** file facilitates establishing connections, sending requests, and receiving responses from the server.
- Once connected, you will receive messages of different types (Action / Event):
    - **Event:** Contains game information such as whose turn it is, command success, and failures.
    - **Action:** Indicates that it is your turn to play, and the server is waiting for your input. An Array is included in the **`Args`** message for use.

## How to Send Input?
- Input is sent using the **`RPCSendMessage(string "methodName", object? args)`** function. To discover available methods, invoke **`RPCSendMessage("Help")`** in the **`Main()`**. The server will return descriptions of how the functions work.

## Where to Begin?
- In the **`ProcessMessage()`** function, an empty switch case for handling methods (Action || Event) exists.
- It's recommended to create a **`GameManager`** class that implements the **`IGameManager`** interface, where you will write your gameplay logic in the **`ProcessRPCMessage(string json)`** function.

## What Do I Code?
- In this Tic-Tac-Toe game, your task is to script the best moves possible to always secure a win. 
- Write a function that determines the optimal move(x,y) to play. Remember, an incorrect position is an automatic loss, so play strategically!

## How to Connect?
- Initiate a connection using the **`ConnectToServerAsync()`** function in **`Main()`**.
    - **Singleplayer** : use "localhost" to play against a bot, which allows you to test your script in a controlled environment.
    - **Multiplayer**  : Enter the server address provided on the Tail'ed website for PvP matchmaking (not yet implemented).

## How the Structure Works:
- Upon launch, **`Program.cs`** connects to the server using **`ConnectToServerAsync()`** within **`Main()`**.
- It also starts an asynchronous task **`ListenForResponsesAsync()`**, which listens to server responses.
- Received responses are handled by **`ProcessMessage()`**, where you decide how to react based on whether it's an "Event" or an "Action".
- The program includes a safeguard that automatically triggers **`Disconnect()`** if a **`ServerClosing`** event is received. This prevents the client from attempting to interact with a non-existent connection.
- Actions determined by your code are sent using **`RPCSendMessage()`**, which converts the message to JSON and sends it through the **`Send()`** function.

Feel free to adapt the system to your preferences if you find a more efficient method.

## MAKE SURE TO INSTALL .NET SDK 8.0!