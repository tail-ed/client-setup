using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Tailed.ProgrammerGames.Connect4

{
    public interface IGameManager
    {
        void ProcessRPCMessage(string json);
    }

    public class ClientRPC
    {
        private static TcpClient? client;
        private static NetworkStream? stream;
        private static IGameManager gameManager = new GameManager();

        private static string UUID = ""; //****************************************

        public static async Task ConnectToServerAsync(string hostname, int port)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(hostname, port);
                stream = client.GetStream();
                _ = ListenForResponsesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect :" + ex);
            }
        }

        private static async Task ListenForResponsesAsync()
        {
            byte[] buffer = new byte[4096];
            StringBuilder responseBuilder = new();
            while (true)
            {
                if (stream == null) throw new ArgumentNullException(nameof(stream));
                try
                {
                    var bytesReceived = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesReceived > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        responseBuilder.Append(response);

                        string[] messages = responseBuilder.ToString().Split('\n');
                        for (int i = 0; i < messages.Length - 1; i++)
                        {
                            string message = messages[i].Trim();
                            if (!string.IsNullOrEmpty(message))
                            {
                                ProcessMessage(message);
                            }
                        }
                        //KEEP THE REMAINING MESSAGES, IF THERE IS ONE
                        responseBuilder = new StringBuilder(messages[^1]);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error receiving data from server : " + e);
                    Disconnect();
                    break;
                }
            }
        }

        private static void ProcessMessage(string jsonMessage)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<dynamic>(jsonMessage);
                if (message == null) return;
                string method = message.Method;
                var args = message.Args;

                switch (method)
                {
                    //IMPLEMENT METHODS HERE
                    case "Login":
                        Console.WriteLine($"[Login]\n{args}\n");
                        RPCSendMessage("Login", new { UUID });
                        break;
                    case "Event":
                        if (message.Args["MethodName"] == "ServerClosing")
                        {
                            Console.WriteLine($"[Event]\n{args}\n");
                            Disconnect();
                        }
                        else
                        {
                            // Console.WriteLine($"[Event]\n{args}\n");
                        }
                        break;
                    case "Help":
                        args = message.Args;
                        Console.WriteLine($"[Help]\n{args}\n");
                        break;
                    default:
                        break;
                }

                gameManager.ProcessRPCMessage(jsonMessage);

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error processing message : " + ex);
            }

        }

        public static void RPCSendMessage(string method, object? args = null)
        {
            try
            {
                //INPUT VALIDATION - CHECK IF NULL OR EMPTY
                if (string.IsNullOrEmpty(method))
                {
                    throw new ArgumentException("Method name cannot be null or empty --> ", nameof(method));
                }

                //CREATE JSON-RPC MESSAGE
                var rpcMessage = new { method = method, args = args };

                //CONVERT MESSAGE TO JSON
                string jsonMessage = JsonConvert.SerializeObject(rpcMessage);
                Send(jsonMessage);

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("RPC MESSAGE ERROR : " + e);
            }
        }

        private static void Send(string jsonMessage)
        {
            if (client == null) return;
            if (stream == null || !client.Connected)
            {
                Console.WriteLine("Failed : Not connected to server");
                return;
            }

            //IF CONNECTED TO SERVER -> SEND REQUEST
            byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
            stream.Write(data, 0, data.Length);
        }

        private static void Disconnect()
        {
            //DISCONNECT FROM SERVER && CLOSE APP
            stream?.Close();
            client?.Close();
            Environment.Exit(1);
        }

        public static async Task Main(string[] args)
        {
            if (args.Length > 0)
            {
                string uuid = args[0];
                UUID = uuid;
                Console.WriteLine("Launching with UUID : " + uuid + "...");
            }
            else
            {
                Console.WriteLine("No UUID provided, closing...");
                Environment.Exit(1);
            }

            //CONNECTION TO SERVER 
            await ConnectToServerAsync("localhost", 25001);

            //FOR COMMANDS LIST, TYPE :
            //RPCSendMessage("Help");

            await Task.Delay(-1);
        }
    }

    //GAMEPLAY EXEMPLE : (BASIC)
    // public class GameManager : IGameManager
    // {

    //     // 0 free
    //     // 1 wall 
    //     // 2 softwall
    //     // 3 bomb
    //     // x player
    //     private static CancellationTokenSource? gameTaskCancellationTokenSource;
    //     private static int[,] gameBoard = new int[6, 7]; // bomberman grid
    //     //private static readonly Random random = new();

    //     // Dictionary to track player positions
    //     private static Dictionary<int, (int x, int y)> playerPositions = new();

    //     public static int playerIdentifier;

    //     public void ProcessRPCMessage(string json)
    //     {
    //         var message = JsonConvert.DeserializeObject<dynamic>(json);
    //         if (message == null) return;

    //         string method = message.Method;
    //         var args = message.Args;

    //         switch (method)
    //         {
    //             case "Event":
    //                 if (args.MethodName != null && args.MethodName == "GameStart")
    //                 {
    //                     HandleGameStart(args);
    //                 }
    //                 else if (args.MethodName != null && args.MethodName == "Move")
    //                 {
    //                     HandleMove(args);
    //                 }
    //                 else if (args.MethodName != null && args.MethodName == "PlacBombeBombBomb")
    //                 {
    //                     HandleBomb(args);
    //                 }
    //                 break;
    //             default:
    //                 Console.WriteLine($"Unhandled message type : {method}");
    //                 break;
    //         }
    //     }

    //     private static void HandleGameStart(dynamic args)
    //     {
    //         // Display action text
    //         // Console.WriteLine($"[Event]\n{args}\n");

    //         // Get game array
    //         string jsonBoard = args.Board;
    //         Console.WriteLine($"Board : {jsonBoard}");
    //         // gameBoard = JsonConvert.DeserializeObject<int[,]>(jsonBoard);
    //         gameBoard = JsonConvert.DeserializeObject<int[,]>(jsonBoard, new IntArray2DConverter());
    //         Console.WriteLine($"Board : {gameBoard}");

    //         string jsonPlayer = args.Player;
    //         playerIdentifier = JsonConvert.DeserializeObject<int>(jsonPlayer);

    //         string jsonX = args.X;
    //         var x = JsonConvert.DeserializeObject<int>(jsonX);

    //         string jsonY = args.Y;
    //         var y = JsonConvert.DeserializeObject<int>(jsonY);

    //         // Print out the game board state for debugging
    //         PrintGameBoard(gameBoard);

    //         // Initialize player positions (assuming the bot is always "bot")
    //         playerPositions[playerIdentifier] = (x, y); // Example starting position

    //         // Start the periodic task to send moves
    //         StartPeriodicMoveTask();
    //         StartPeriodicBombTask();

    //     }

    //     private static void StartPeriodicMoveTask()
    //     {
    //         // Cancel any existing task
    //         gameTaskCancellationTokenSource?.Cancel();

    //         // Create a new CancellationTokenSource
    //         gameTaskCancellationTokenSource = new CancellationTokenSource();
    //         var token = gameTaskCancellationTokenSource.Token;

    //         // Start a new background task
    //         Task.Run(async () =>
    //         {
    //             while (!token.IsCancellationRequested)
    //             {
    //                 try
    //                 {
    //                     // Get the best move
    //                     string direction = GetBestMove(gameBoard);

    //                     // Send the move to the server
    //                     if (!string.IsNullOrEmpty(direction))
    //                     {
    //                         ClientRPC.RPCSendMessage("Move", new { Direction = direction });
    //                         Console.WriteLine($"Sending Move: {direction}");
    //                     }

    //                     // Wait for a specified period (e.g., 5 second)
    //                     await Task.Delay(1000, token);
    //                 }
    //                 catch (TaskCanceledException)
    //                 {
    //                     // Task was canceled, exit the loop
    //                     break;
    //                 }
    //             }
    //         }, token);
    //     }

    //     private static void StartPeriodicBombTask()
    //     {
    //         // Cancel any existing task
    //         gameTaskCancellationTokenSource?.Cancel();

    //         // Create a new CancellationTokenSource
    //         gameTaskCancellationTokenSource = new CancellationTokenSource();
    //         var token = gameTaskCancellationTokenSource.Token;

    //         // Start a new background task
    //         Task.Run(async () =>
    //         {
    //             while (!token.IsCancellationRequested)
    //             {
    //                 try
    //                 {
    //                     // Send the bomb to the server
    //                     ClientRPC.RPCSendMessage("PlaceBomb");
    //                     Console.WriteLine($"Sending PlaceBomb");

    //                     // Wait for a specified period (e.g., 5 second)
    //                     await Task.Delay(5000, token);
    //                 }
    //                 catch (TaskCanceledException)
    //                 {
    //                     // Task was canceled, exit the loop
    //                     break;
    //                 }
    //             }
    //         }, token);
    //     }

    //     private static void HandleMove(dynamic args)
    //     {
    //         // Display action text
    //         Console.WriteLine($"[Event]\n{args}\n");

    //         // Extract player ID and direction
    //         int playerId = args.Player;
    //         string direction = args.Direction;

    //         // Update player position
    //         if (playerPositions.ContainsKey(playerId))
    //         {
    //             var position = playerPositions[playerId];
                
    //             int previousX = position.x;
    //             int previousY = position.y;

    //             switch (direction)
    //             {
    //                 case "UP":
    //                     position.y += 1;
    //                     break;
    //                 case "DOWN":
    //                     position.y -= 1;
    //                     break;
    //                 case "LEFT":
    //                     position.x -= 1;
    //                     break;
    //                 case "RIGHT":
    //                     position.x += 1;
    //                     break;
    //             }

    //             // Ensure the new position is valid
    //             if (IsValidMove(gameBoard, position.x, position.y))
    //             {
    //                 playerPositions[playerId] = position;
    //                 gameBoard[previousX, previousY] -= playerId;
    //                 gameBoard[position.x, position.y] += playerId;
    //             }
    //         }
    //     }

    //     private static void HandleBomb(dynamic args)
    //     {
    //         // Display action text
    //         Console.WriteLine($"[Event]\n{args}\n");

    //         // Extract player ID and direction
    //         int playerId = args.Player;

    //         // Update player position
    //         if (playerPositions.ContainsKey(playerId))
    //         {
    //             var playerPostion = playerPositions[playerId];
    //             var x = playerPostion.x;
    //             var y = playerPostion.y;
    //             gameBoard[x, y] += 3;
    //         }
    //     }

    //     private static string GetBestMove(int[,] board)
    //     {
    //         var random = new Random();
    //         List<string> validMoves = new();

    //         // Get the bot's position (assuming the bot is always "bot")
    //         var botPosition = playerPositions[playerIdentifier];

    //         // Define possible moves (up, down, left, right) with corresponding directions
    //         var possibleMoves = new List<(int x, int y, string direction)>
    //         {
    //             (botPosition.x, botPosition.y + 1, "UP"),
    //             (botPosition.x, botPosition.y - 1, "DOWN"),
    //             (botPosition.x - 1, botPosition.y, "LEFT"),
    //             (botPosition.x + 1, botPosition.y, "RIGHT")
    //         };

    //         foreach (var move in possibleMoves)
    //         {
    //             if (IsValidMove(board, move.x, move.y))
    //             {
    //                 validMoves.Add(move.direction);
    //                 Console.WriteLine($"Valid Move : {move.direction} {move.x} {move.y}");
    //             }
    //         }

    //         // If there are valid moves, return a random valid move
    //         if (validMoves.Count > 0)
    //         {
    //             return validMoves[random.Next(validMoves.Count)];
    //         }

    //         // Return an empty string if no valid moves are found
    //         return string.Empty;
    //     }


    //     private static bool IsValidMove(int[,] board, int x, int y)
    //     {
    //         // Ensure coordinates are within bounds and space is free
    //         return x >= 0 && x < board.GetLength(0) && y >= 0 && y < board.GetLength(1) && board[x, y] == 0;
    //     }

    //     private static void PrintGameBoard(int[,] board)
    //     {
    //         // Get the dimensions of the board
    //         int rows = board.GetLength(0);
    //         int cols = board.GetLength(1);

    //         Console.WriteLine($"Board Rows={rows} Columns={cols}");

    //         // Iterate over the rows in reverse order to print from bottom to top
            
    //         for (int col = cols - 1 ; col >= 0; col--)
    //         {
    //             for (int row = 0; row < rows; row++)
    //             {
    //                 // Print each value with a width of 3 characters, right-aligned
    //                 Console.Write(board[row, col].ToString().PadLeft(3) + " ");
    //             }
    //             Console.WriteLine();
    //         }
    //     }
    // }

    public enum BotState
    {
        MOVE_TO_SOFT_WALL,
        PLACE_BOMB,
        MOVE_TO_SAFE_LOCATION,
        IDLE
    }

    public class PlayerState
    {
        public int MaxBombCount { get; set; }
        public int BombCount { get; set; }
        public int ExplosionRadius { get; set; }

        public PlayerState(int maxBombCount = 1, int bombCount = 1, int explosionRadius = 2)
        {
            MaxBombCount = maxBombCount;
            BombCount = bombCount;
            ExplosionRadius = explosionRadius;
        }
    }

    public class Bomb
    {
        public int PlayerIdentifier { get; set; }
        public int ExplosionRadius { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public Bomb(int playerIdentifier, int explosionRadius, int x, int y)
        {
            PlayerIdentifier = playerIdentifier;
            ExplosionRadius = explosionRadius;
            X = x;
            Y = y;
        }
    }

    public class GameManager : IGameManager
    {
        // 0 free
        // 1 wall 
        // 2 softwall
        // 3 bomb
        // x player
        private static CancellationTokenSource? gameTaskCancellationTokenSource;
        private static int[,] gameBoard;

        // Dictionary to track player positions
        private static Dictionary<int, (int x, int y)> playerPositions = new();
        private static List<Bomb> bombPositions = new(); // To track where bombs are placed
        private static Bomb waitToPlantBomb = null;
        private static Dictionary<int, (int x, int y)> safePositions = new(); // To track where safe positions are
        private static Dictionary<int, PlayerState> playerStates = new();

        public static int playerIdentifier;

        private const int ExplosionRadius = 2; // Define the explosion radius

        public static BotState botState = BotState.MOVE_TO_SOFT_WALL;

        public void ProcessRPCMessage(string json)
        {
            var message = JsonConvert.DeserializeObject<dynamic>(json);
            if (message == null) return;

            string method = message.Method;
            var args = message.Args;

            switch (method)
            {
                case "Event":
                    if (args.MethodName != null && args.MethodName == "GameStart")
                    {
                        HandleGameStart(args);
                    }
                    else if (args.MethodName != null && args.MethodName == "Move")
                    {
                        HandleMove(args);
                    }
                    else if (args.MethodName != null && args.MethodName == "Bomb")
                    {
                        HandleBomb(args);
                    }
                    else if (args.MethodName != null && args.MethodName == "BombExploded")
                    {
                        HandleBombExploded(args);
                    }
                    else if (args.MethodName != null && args.MethodName == "PlayerDied")
                    {
                        HandlePlayerDied(args);
                    }
                    else if (args.MethodName != null && args.MethodName == "BonusSpawned")
                    {
                        HandleBonusSpawned(args);
                    }
                    else if (args.MethodName != null && args.MethodName == "CollectBonus")
                    {
                        HandleCollectBonus(args);
                    }
                    break;
                default:
                    Console.WriteLine($"Unhandled message type : {method}");
                    break;
            }
        }

        private static void HandleGameStart(dynamic args)
        {
            // Get game array
            string jsonBoard = args.Board;
            Console.WriteLine($"Board : {jsonBoard}");
            gameBoard = JsonConvert.DeserializeObject<int[,]>(jsonBoard);
            Console.WriteLine($"Board : {gameBoard}");

            string jsonPlayer = args.Player;
            playerIdentifier = JsonConvert.DeserializeObject<int>(jsonPlayer);

            string jsonPlayers = args.Players;
            var players = JsonConvert.DeserializeObject<List<(int,int,int)>>(jsonPlayers);

            // Print out the game board state for debugging
            PrintGameBoard(gameBoard);

            foreach (var (player, x, y) in players) 
            {
                // Initialize player positions
                playerPositions[player] = (x, y);

                playerStates[player] = new PlayerState();
            }
            
            // Start the periodic task to handle bot actions
            StartBotTasks();
        }

        private static void StartBotTasks()
        {
            // Cancel any existing task
            gameTaskCancellationTokenSource?.Cancel();

            // Create a new CancellationTokenSource
            gameTaskCancellationTokenSource = new CancellationTokenSource();
            var token = gameTaskCancellationTokenSource.Token;

            // Start a new background task for handling bot actions
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        switch (botState)
                        {
                            case BotState.MOVE_TO_SOFT_WALL:
                                await Task.Delay(1500, token);
                                if (TryMoveToSoftWall(out var softWallPosition))
                                {
                                    // Move to soft wall
                                    MoveToPosition(softWallPosition);

                                    Console.WriteLine($"Sending PlaceBomb at position: ({softWallPosition.x}, {softWallPosition.y})");

                                    // Store bomb position
                                    var playerPosition = playerPositions[playerIdentifier];
                                    
                                    if (softWallPosition.x == playerPosition.x && softWallPosition.y == playerPosition.y) 
                                    {
                                        ClientRPC.RPCSendMessage("PlaceBomb");
                                        // gameBoard[bombPosition.x, bombPosition.y] += 3;
                                        Console.WriteLine($"Placing Bomb: ({playerPosition.x}, {playerPosition.y})");
                                        // bombPositions.Add(new Bomb(playerIdentifier, playerStates[playerIdentifier].ExplosionRadius, playerPosition.x, playerPosition.y));
                                        botState = BotState.MOVE_TO_SAFE_LOCATION;
                                    }
                                    else {
                                        waitToPlantBomb = new Bomb(playerIdentifier, playerStates[playerIdentifier].ExplosionRadius, softWallPosition.x, softWallPosition.y);
                                        botState = BotState.PLACE_BOMB;
                                    }
                                }
                                else 
                                {
                                    PrintGameBoard(gameBoard);
                                }
                                break;

                            case BotState.PLACE_BOMB:
                                // Place bomb logic (if needed)
                                // BotState = BotState.MOVE_TO_SAFE_LOCATION;
                                break;

                            case BotState.MOVE_TO_SAFE_LOCATION:
                                if (TryMoveToSafeLocation(out var safePosition))
                                {
                                    // Move to safe location
                                    MoveToPosition(safePosition);
                                    botState = BotState.IDLE; // Or continue the logic as needed
                                    safePositions[playerIdentifier] = safePosition;
                                }
                                else 
                                {
                                    PrintGameBoard(gameBoard);
                                }
                                break;

                            case BotState.IDLE:
                                // Idle state logic (if any)
                                break;
                        }

                        // Wait for a specified period before next action
                        await Task.Delay(200, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Task was canceled, exit the loop
                        break;
                    }
                }
            }, token);
        }

        private static void MoveToPosition((int x, int y) targetPosition)
        {
            var botPosition = playerPositions[playerIdentifier];
            Console.WriteLine($"Start MoveToPosition from {botPosition} to {targetPosition}");
            var path = GetPathTo(botPosition, targetPosition);

            Console.WriteLine($"Move in {path.Count} positions");
            // Send each move command to the server
            foreach (var (x, y, direction) in path)
            {
                Console.WriteLine($"Moving {direction} to position: ({x}, {y})");
                ClientRPC.RPCSendMessage("Move", new { Direction = direction });
            }
        }
        
        private static List<(int x, int y)> FindAllSoftWalls(int[,] board, (int x, int y) botPosition)
        {
            List<(int x, int y)> softWalls = new();

            // Directions: up, down, left, right
            var directions = new (int dx, int dy)[]
            {
                (0, 1),  // Up
                (0, -1), // Down
                (-1, 0), // Left
                (1, 0)   // Right
            };

            // Queue for BFS
            var queue = new Queue<((int x, int y) position, (int x, int y) direction)>();
            var visited = new HashSet<(int x, int y)>();

            // Enqueue all initial positions with their directions
            foreach (var (dx, dy) in directions)
            {
                queue.Enqueue((botPosition, (dx, dy)));
                visited.Add(botPosition);
            }

            while (queue.Count > 0)
            {
                var (current, (dx, dy)) = queue.Dequeue();
                var (currentX, currentY) = current;

                int x = currentX;
                int y = currentY;

                // Move in the current direction until hitting a block or boundary
                while (true)
                {
                    int previousX = x;
                    int previousY = y;

                    x += dx;
                    y += dy;

                    // Check boundaries
                    if (x < 0 || x >= board.GetLength(0) || y < 0 || y >= board.GetLength(1))
                        break;

                    // Check if the current cell is a soft wall
                    if (board[x, y] == 2)
                    {
                        // Add the soft wall to the list
                        softWalls.Add((previousX, previousY));
                        break; // Stop checking this direction after finding the first soft wall
                    }
                    // If the current cell is a wall or bomb, stop checking further in this direction
                    else if (board[x, y] == 1 || board[x, y] % 100 == 3)
                    {
                        break;
                    }

                    // Continue moving in the current direction
                    if (!visited.Contains((x, y)))
                    {
                        visited.Add((x, y));
                        foreach (var (alldx, alldy) in directions)
                        {
                            queue.Enqueue(((x,y), (alldx, alldy)));
                        }
                    }
                }
            }
            return softWalls;
        }

        private static bool TryMoveToSoftWall(out (int x, int y) softWallPosition)
        {
            Console.WriteLine($"Try Move to Soft Wall");
            var botPosition = playerPositions[playerIdentifier];
            
            var softWalls = FindAllSoftWalls(gameBoard, botPosition);

            // Move to the closest soft wall
            if (softWalls.Count > 0)
            {
                // Find the closest soft wall
                softWallPosition = softWalls
                    .OrderBy(p => Math.Abs(p.x - botPosition.x) + Math.Abs(p.y - botPosition.y))
                    .First();
                
                return true;
            }

            // No soft wall found
            softWallPosition = (-1, -1);
            return false;
        }

        // Method to get the path from the current position to the target position
        private static List<(int x, int y, string direction)> GetPathTo((int x, int y) start, (int x, int y) end)
        {
            // Console.WriteLine($"Start GetPathTo {start} {end}");
            var path = new List<(int x, int y, string direction)>();

            // Directions for movement
            var directions = new (int dx, int dy, string direction)[]
            {
                (0, 1, "UP"),
                (0, -1, "DOWN"),
                (-1, 0, "LEFT"),
                (1, 0, "RIGHT")
            };

            // Queue for BFS
            var queue = new Queue<((int x, int y) position, List<(int x, int y, string direction)> moves)>();
            var visited = new HashSet<(int x, int y)>();

            // Enqueue start position
            queue.Enqueue((start, new List<(int x, int y, string direction)>()));
            visited.Add(start);

            while (queue.Count > 0)
            {
                var (current, currentPath) = queue.Dequeue();
                var (cx, cy) = current;

                // If the destination is reached
                if (current == end)
                {
                    path = currentPath;
                    break;
                }

                foreach (var (dx, dy, dir) in directions)
                {
                    var newX = cx + dx;
                    var newY = cy + dy;
                    var newPos = (newX, newY);

                    // Console.WriteLine($"[GetPathTo] isValid Move : {newX} {newY} {IsValidMove(gameBoard, newX, newY)}");

                    if (IsValidMove(gameBoard, newX, newY) && !visited.Contains(newPos))
                    {
                        var newPath = new List<(int x, int y, string direction)>(currentPath)
                        {
                            (newX, newY, dir)
                        };

                        queue.Enqueue((newPos, newPath));
                        visited.Add(newPos);
                    }
                }
            }

            // Console.WriteLine($"[GetPathTo] Go to location in {path.Count} moves");

            return path;
        }

        private static bool TryMoveToSafeLocation(out (int x, int y) safePosition)
        {
            var botPosition = playerPositions[playerIdentifier];
            Console.WriteLine($"Try Move to Safe Location from {botPosition.x} {botPosition.y}");

            // Directions for movement
            var directions = new (int dx, int dy)[]
            {
                (0, 1),
                (0, -1),
                (-1, 0),
                (1, 0)
            };

            // Queue for BFS
            var queue = new Queue<(int x, int y)>();
            var visited = new HashSet<(int x, int y)>();

            // Enqueue start position
            queue.Enqueue(botPosition);
            visited.Add(botPosition);

            while (queue.Count > 0)
            {
                var (currentX, currentY) = queue.Dequeue();

                // Check if the current position is a valid and safe location
                if (IsValidMove(gameBoard, currentX, currentY) && !IsDangerZone(gameBoard, currentX, currentY))
                {
                    safePosition = (currentX, currentY);
                    Console.WriteLine($"Picked Safe Location : {safePosition}");
                    return true;
                }

                // Enqueue valid neighboring positions
                foreach (var (dx, dy) in directions)
                {
                    var newX = currentX + dx;
                    var newY = currentY + dy;
                    var newPos = (newX, newY);

                    if (IsValidMove(gameBoard, newX, newY) && !visited.Contains(newPos))
                    {
                        queue.Enqueue(newPos);
                        visited.Add(newPos);
                    }
                }
            }

            // No safe location found
            safePosition = (-1, -1);
            return false;
        }

        private static bool IsDangerZone(int[,] board, int x, int y)
        {
            Console.WriteLine($"IsDangerZone BombCount {bombPositions.Count} ");
            // Iterate over the entire board to find bombs
            foreach (Bomb bomb in bombPositions)
            {
                int bombX = bomb.X;
                int bombY = bomb.Y;
                
                Console.WriteLine($"bomb at position {bombX} {bombY} ");

                // Check if the position (x, y) is within the explosion radius of the bomb
                // Console.WriteLine($"IsDangerZone For {x} {y} Bomb is on position {bombX} {bombY} Condition - {Math.Abs(bombY - y)} {Math.Abs(bombX - x)}");
                if ((bombX == x && Math.Abs(bombY - y) <= bomb.ExplosionRadius) || 
                    (bombY == y && Math.Abs(bombX - x) <= bomb.ExplosionRadius))
                {
                    return true; // Position is in a danger zone
                }
            }

            return false; // Position is not in a danger zone
        }

        private static void HandleMove(dynamic args)
        {
            // Display action text
            Console.WriteLine($"[Event]\n{args}\n");

            // Extract player ID and direction
            int playerId = args.Player;
            string direction = args.Direction;

            // Update player position
            if (playerPositions.ContainsKey(playerId))
            {
                var position = playerPositions[playerId];
                
                int previousX = position.x;
                int previousY = position.y;

                switch (direction)
                {
                    case "UP":
                        position.y += 1;
                        break;
                    case "DOWN":
                        position.y -= 1;
                        break;
                    case "LEFT":
                        position.x -= 1;
                        break;
                    case "RIGHT":
                        position.x += 1;
                        break;
                }

                // Ensure the new position is valid
                if (IsValidMove(gameBoard, position.x, position.y))
                {
                    playerPositions[playerId] = position;
                    gameBoard[previousX, previousY] -= playerId;
                    gameBoard[position.x, position.y] += playerId;

                    // If playerId is equal to player identifier and location is equal to placebomb location then call ClientRPC.RPCSendMessage("PlaceBomb");
                    if (playerId == playerIdentifier)
                    {
                        // Console.WriteLine($"Check State {botState} {position} {bombPosition} {position.x == bombPosition.x} {position.y == bombPosition.y}");
                        if (botState == BotState.PLACE_BOMB && waitToPlantBomb != null && position.x == waitToPlantBomb.X && position.y == waitToPlantBomb.Y)
                        {
                            ClientRPC.RPCSendMessage("PlaceBomb");
                            // gameBoard[bombPosition.x, bombPosition.y] += 3;
                            Console.WriteLine($"Placing Bomb: ({waitToPlantBomb.X}, {waitToPlantBomb.Y})");
                            // bombPositions.Add(new Bomb(playerIdentifier, playerStates[playerIdentifier].ExplosionRadius, position.x, position.y));
                        }
                        // else if (botState == BotState.IDLE && safePositions.TryGetValue(playerId, out var safePosition) && position.x == safePosition.x && position.y == safePosition.y) {
                        //     botState = BotState.MOVE_TO_SOFT_WALL;
                        // }
                    }
                }
            }
        }

        private static void HandleBomb(dynamic args)
        {
            // Display action text
            Console.WriteLine($"[Event]\n{args}\n");

            // Extract player ID and direction
            int playerId = args.Player;
            int radius = args.Radius;

            // Update player position
            if (playerPositions.ContainsKey(playerId))
            {
                var playerPosition = playerPositions[playerId];
                var x = playerPosition.x;
                var y = playerPosition.y;
                gameBoard[x, y] += 3;
                bombPositions.Add(new Bomb(playerId, playerStates[playerId].ExplosionRadius, x, y));
                if (playerId == playerIdentifier) 
                {
                    botState = BotState.MOVE_TO_SAFE_LOCATION;
                }
            }
        }

        private static void HandleBombExploded(dynamic args)
        {
            // Display action text
            Console.WriteLine($"[Event]\n{args}\n");

            // Get affected coordinates from the args
            string jsonAffectedCoordinates = args.AffectedCoordinates;
            Console.WriteLine($"Affected Coordinates : {jsonAffectedCoordinates}");
            
            // Deserialize the affected coordinates
            var affectedCoordinates = JsonConvert.DeserializeObject<List<(int x, int y)>>(jsonAffectedCoordinates);
            
            string jsonOwner = args.Owner;
            var bombOwner = JsonConvert.DeserializeObject<int>(jsonOwner);

            string jsonX = args.X;
            var bombX = JsonConvert.DeserializeObject<int>(jsonX);
            
            string jsonY = args.Y;
            var bombY = JsonConvert.DeserializeObject<int>(jsonY);

            // Reset the game board at the affected coordinates
            foreach (var (x, y) in affectedCoordinates)
            {
                if (x >= 0 && x < gameBoard.GetLength(0) && y >= 0 && y < gameBoard.GetLength(1))
                {
                    gameBoard[x, y] = 0;
                    // Console.WriteLine($"Resetting board position: ({x}, {y}) to 0");
                }
            }

            playerStates[bombOwner].BombCount = Math.Min(playerStates[bombOwner].BombCount + 1, playerStates[bombOwner].MaxBombCount);

            RemoveBomb(bombOwner);

            if (playerIdentifier == bombOwner) 
            {
                botState = BotState.MOVE_TO_SOFT_WALL;
            }
        }

        private static void RemoveBomb(int bombOwner)
        {
            var bombToRemove = bombPositions
                .FirstOrDefault(b => b.PlayerIdentifier == bombOwner);

            if (bombToRemove != null)
            {
                bombPositions.Remove(bombToRemove);
                Console.WriteLine($"Removed bomb position for owner: {bombOwner}");
            }
        }
        private static void HandleCollectBonus(dynamic args)
        {
            int playerId = JsonConvert.DeserializeObject<int>(args.Player.ToString());
            int bonusType = JsonConvert.DeserializeObject<int>(args.Bonus.ToString());
            int x = JsonConvert.DeserializeObject<int>(args.X.ToString());
            int y = JsonConvert.DeserializeObject<int>(args.Y.ToString());

            gameBoard[x, y] -= bonusType;

            var playerState = playerStates[playerId];
            // public int MaxBombCount { get; set; }
            // public int BombCount { get; set; }
            // public int ExplosionRadius { get; set; }

            Console.WriteLine($"Player {playerId} collected bonus {bonusType} at {x} {y}");

            switch (bonusType)
            {
                case 10:
                    playerState.ExplosionRadius = Math.Min(playerState.ExplosionRadius + 1, 4); // Cap at 4
                    break;
                case 11:
                    playerState.ExplosionRadius = 4; // Set to max 4
                    break;
                case 12:
                    playerState.ExplosionRadius = Math.Max(playerState.ExplosionRadius - 1, 1); // Cap at 1
                    break;
                case 13:
                    playerState.MaxBombCount = Math.Min(playerState.MaxBombCount + 1, 3); // Cap at 3
                    playerState.BombCount += 1;
                    break;
                case 14:
                    playerState.MaxBombCount = Math.Max(playerState.MaxBombCount - 1, 1); // Cap at 1
                    break;
                // Handle other collected bonuses as needed
            }

            playerState.BombCount = Math.Min(playerState.BombCount, playerState.MaxBombCount);
        }

        private static void HandleBonusSpawned(dynamic args)
        {
            int bonusType = JsonConvert.DeserializeObject<int>(args.Bonus.ToString());

            int x = JsonConvert.DeserializeObject<int>(args.X.ToString());
            int y = JsonConvert.DeserializeObject<int>(args.Y.ToString());

            Console.WriteLine($"Bonus Spawned at {x} {y}");

            gameBoard[x, y] = bonusType;
        }

        private static void HandlePlayerDied(dynamic args)
        {
            int playerId = JsonConvert.DeserializeObject<int>(args.Player.ToString());
            int x = JsonConvert.DeserializeObject<int>(args.X.ToString());
            int y = JsonConvert.DeserializeObject<int>(args.Y.ToString());

            gameBoard[x, y] -= playerId;
            
            Console.WriteLine($"Player {playerId} died at {x} {y}");

            playerPositions.Remove(playerId);
        }

        private static string GetBestMove(int[,] board)
        {
            var random = new Random();
            List<string> validMoves = new();

            // Get the bot's position
            var botPosition = playerPositions[playerIdentifier];

            // Define possible moves (up, down, left, right) with corresponding directions
            var possibleMoves = new List<(int x, int y, string direction)>
            {
                (botPosition.x, botPosition.y + 1, "UP"),
                (botPosition.x, botPosition.y - 1, "DOWN"),
                (botPosition.x - 1, botPosition.y, "LEFT"),
                (botPosition.x + 1, botPosition.y, "RIGHT")
            };

            foreach (var move in possibleMoves)
            {
                if (IsValidMove(board, move.x, move.y))
                {
                    validMoves.Add(move.direction);
                    Console.WriteLine($"Valid Move : {move.direction} {move.x} {move.y}");
                }
            }

            // If there are valid moves, return a random valid move
            if (validMoves.Count > 0)
            {
                return validMoves[random.Next(validMoves.Count)];
            }

            // Return an empty string if no valid moves are found
            return string.Empty;
        }

        private static bool IsValidMove(int[,] board, int x, int y)
        {
            // Check if coordinates are outside of board boundaries
            if (x < 0 || x >= board.GetLength(0) || y < 0 || y >= board.GetLength(1))
            {
                return false;
            }

            // Get the board value at the given coordinates
            int boardValue = board[x, y];

            // Ensure space is free (value is 0) or within valid range for movement
            return boardValue == 0 || (boardValue >= 10 && boardValue < 100);
        }

        private static void PrintGameBoard(int[,] board)
        {
            // Get the dimensions of the board
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            Console.WriteLine($"Board Rows={rows} Columns={cols}");

            // Iterate over the rows in reverse order to print from bottom to top
            
            for (int col = cols - 1 ; col >= 0; col--)
            {
                for (int row = 0; row < rows; row++)
                {
                    // Print each value with a width of 3 characters, right-aligned
                    Console.Write(board[row, col].ToString().PadLeft(3) + " ");
                }
                Console.WriteLine();
            }
        }
    }
}