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

    public class IntArray2DConverter : JsonConverter<int[,]>
    {
        public override void WriteJson(JsonWriter writer, int[,] value, JsonSerializer serializer)
        {
            JArray array = new JArray();
            for (int i = 0; i < value.GetLength(0); i++)
            {
                JArray row = new JArray();
                for (int j = 0; j < value.GetLength(1); j++)
                {
                    row.Add(value[i, j]);
                }
                array.Add(row);
            }
            array.WriteTo(writer);
        }

        public override int[,] ReadJson(JsonReader reader, Type objectType, int[,] existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray array = JArray.Load(reader);
            int rows = array.Count;
            int cols = array[0].Count();
            int[,] result = new int[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = (int)array[i][j];
                }
            }
            return result;
        }
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
    public class GameManager : IGameManager
    {

        // 0 free
        // 1 wall 
        // 2 softwall
        // 3 bomb
        // x player
        private static CancellationTokenSource? gameTaskCancellationTokenSource;
        private static int[,] gameBoard = new int[6, 7]; // bomberman grid
        //private static readonly Random random = new();

        // Dictionary to track player positions
        private static Dictionary<int, (int x, int y)> playerPositions = new();

        public static int playerIdentifier;

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
                    break;
                default:
                    Console.WriteLine($"Unhandled message type : {method}");
                    break;
            }
        }

        private static void HandleGameStart(dynamic args)
        {
            // Display action text
            // Console.WriteLine($"[Event]\n{args}\n");

            // Get game array
            string jsonBoard = args.Board;
            Console.WriteLine($"Board : {jsonBoard}");
            // gameBoard = JsonConvert.DeserializeObject<int[,]>(jsonBoard);
            gameBoard = JsonConvert.DeserializeObject<int[,]>(jsonBoard, new IntArray2DConverter());
            Console.WriteLine($"Board : {gameBoard}");

            string jsonPlayer = args.Player;
            playerIdentifier = JsonConvert.DeserializeObject<int>(jsonPlayer);

            string jsonX = args.X;
            var x = JsonConvert.DeserializeObject<int>(jsonX);

            string jsonY = args.Y;
            var y = JsonConvert.DeserializeObject<int>(jsonY);

            // Print out the game board state for debugging
            PrintGameBoard(gameBoard);

            // Initialize player positions (assuming the bot is always "bot")
            playerPositions[playerIdentifier] = (x, y); // Example starting position

            // Start the periodic task to send moves
            StartPeriodicMoveTask();

        }

        private static void StartPeriodicMoveTask()
        {
            // Cancel any existing task
            gameTaskCancellationTokenSource?.Cancel();

            // Create a new CancellationTokenSource
            gameTaskCancellationTokenSource = new CancellationTokenSource();
            var token = gameTaskCancellationTokenSource.Token;

            // Start a new background task
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // Get the best move
                        string direction = GetBestMove(gameBoard);

                        // Send the move to the server
                        if (!string.IsNullOrEmpty(direction))
                        {
                            ClientRPC.RPCSendMessage("Move", new { Direction = direction });
                            Console.WriteLine($"Sending Move: {direction}");
                        }

                        // Wait for a specified period (e.g., 5 second)
                        await Task.Delay(5000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Task was canceled, exit the loop
                        break;
                    }
                }
            }, token);
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
                    gameBoard[previousX, previousY] = 0;
                    gameBoard[position.x, position.y] = playerId;
                }
            }

        }

        private static string GetBestMove(int[,] board)
        {
            var random = new Random();
            List<string> validMoves = new();

            // Get the bot's position (assuming the bot is always "bot")
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
            // Ensure coordinates are within bounds and space is free
            return x >= 0 && x < board.GetLength(0) && y >= 0 && y < board.GetLength(1) && board[x, y] == 0;
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