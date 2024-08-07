using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;

namespace Tailed.Games.Snake
{
    public interface IGameManager
    {
        void ProcessRPCMessage(string json);
    }

#region ClientRpc Basic Functions

    public class ClientRPC
    {
        private static TcpClient? client;
        private static NetworkStream? stream;
        private static IGameManager gameManager = new GameManager();

        private static string UUID = "2"; //****************************************

        public static async Task ConnectToServerAsync(string hostname, int port)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(hostname, port);
                stream = client.GetStream();
                _ = ListenForResponsesAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to connect :" + ex.Message);
            }
        }

        private static async Task ListenForResponsesAsync()
        {
            byte[] buffer = new byte[4096];
            StringBuilder responseBuilder = new();
            while(true)
            {
                if(stream == null) throw new ArgumentNullException(nameof(stream));
                try
                {
                    var bytesReceived = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if(bytesReceived > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        responseBuilder.Append(response);
                        
                        string[] messages = responseBuilder.ToString().Split('\n');
                        for(int i = 0; i < messages.Length - 1; i++)
                        {
                            string message = messages[i].Trim();
                            if(!string.IsNullOrEmpty(message))
                            {
                                ProcessMessage(message);
                            }
                        }
                        //KEEP THE REMAINING MESSAGES, IF THERE IS ONE
                        responseBuilder = new StringBuilder(messages[^1]);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error receiving data from server : " + e.Message);
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
                if(message == null) return;
                string method = message.Method;
                var args = message.Args;

                switch(method)
                {
                    //IMPLEMENT METHODS HERE
                    case "Login":
                        Console.WriteLine($"[Login]\n{args}\n");
                        RPCSendMessage("Login", new {UUID});
                        break;
                    case "Event":
                        if(message.Args["MethodName"] == "ServerClosing")
                        {
                            Console.WriteLine($"[Event]\n{args}\n");
                            Disconnect();
                        }
                        else
                        {
                            Console.WriteLine($"[Event]\n{args}\n");
                        }
                        break;
                    case "Help":
                        args = message.Args;
                        Console.WriteLine($"[Help]\n{args}\n");
                        break;
                    default:
                        gameManager.ProcessRPCMessage(jsonMessage);
                        break;
                }
            } catch(Exception ex)
            {
                Console.Error.WriteLine("Error processing message : " + ex.Message);
            }

        }

        public static void RPCSendMessage(string method, object? args = null)
        {
            try
            {
                //INPUT VALIDATION - CHECK IF NULL OR EMPTY
                if(string.IsNullOrEmpty(method))
                {
                    throw new ArgumentException("Method name cannot be null or empty --> ", nameof(method));
                }

                //CREATE JSON-RPC MESSAGE
                var rpcMessage = new { method = method, args = args };

                //CONVERT MESSAGE TO JSON
                string jsonMessage = JsonConvert.SerializeObject(rpcMessage);
                Send(jsonMessage);

            }
            catch(Exception e)
            {
                Console.Error.WriteLine("RPC MESSAGE ERROR : " + e.Message);
            }
        }

        private static void Send(string jsonMessage)
        {
            if(client == null) return;
            if(stream == null || !client.Connected)
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
            /*
            if(args.Length > 0)
            {
                string uuid = args[0];
                UUID = uuid;
                Console.WriteLine("Launching with UUID : " + uuid + "...");
            }
            else
            {
                Console.WriteLine("No UUID provided, closing...");
                Environment.Exit(1);
            }*/

            //CONNECTION TO SERVER
            //await ConnectToServerAsync("socket.tictactoe.tailed.ca",25001);
            await ConnectToServerAsync("localhost", 25001);
    
            //FOR COMMANDS LIST, TYPE :
            //RPCSendMessage("Help");
            
            await Task.Delay(-1);
        }
    }
    #endregion

    //GAMEPLAY EXEMPLE : (BASIC)
    public class GameManager : IGameManager
    {
        private static char[,] gameArray = new char[16,9];
        private static bool loopFinish = false;
        private static List<Vector2> snakePositions;
        private static readonly Random random = new();

        public void ProcessRPCMessage(string json)
        {
            var message = JsonConvert.DeserializeObject<dynamic>(json);
            if(message == null) return;

            string method = message.Method;
            var args = message.Args;

            switch(method)
            {
                case "Action":
                    HandleAction(args);
                    break;
                default:
                    Console.WriteLine($"Unhandled message type : {method}");
                    break;
            }
        }

        private static void HandleAction(dynamic args)
        {
            try
            {
                //DISPLAY ACTION TEXT
                Console.WriteLine($"[Action]\n{args}\n");
                
                //EXTRACT ARGS FROM SERVER
                ExtractPositions(args);

                //LA LOGIQUE DE DÉPLACEMENT DOIT SE FAIRE ICI
                Queue<Vector2> directions = GetMoveSnakeAI(gameArray);
                PrintQueue(directions);

                //ENVOIE DE LA DIRECTION QUEUE ICI
                ClientRPC.RPCSendMessage("SendDirections", new { directions });
                Console.WriteLine("Sending Directions...");
                //
            }
            catch(Exception e)
            {
                Console.WriteLine($"{e}");
            }
        }

        private static void PrintQueue(Queue<Vector2> directions)
        {
            Console.WriteLine($"Direction Queue : ({directions.Count})\n");
            foreach (var direction in directions)
            {
                Console.WriteLine($"({direction.X}, {direction.Y})");
            }
        }

        private static Queue<Vector2> GetMoveSnakeAI(char[,] gameArray)
        {
            Queue<Vector2> directions = new();

            // Supposons que la tête du serpent soit la première position de la liste
            Vector2 snakeHead = snakePositions[0];
            Console.WriteLine($"head pos = x{snakeHead.X} y{snakeHead.Y}");
            Console.WriteLine($"loopfinished? = {loopFinish}");
            Console.WriteLine($"mapSize = {gameArray.GetLength(0)} x {gameArray.GetLength(1)}");

            // Exemples de logique de mouvement
            if(loopFinish)
            {
                if (snakeHead.X == 0 && snakeHead.Y == 0) // position = 0,0
                {
                    loopFinish = false;
                    directions.Enqueue(new Vector2(1, 0)); // GoRight
                }
                else if (snakeHead.X == 0 && snakeHead.Y > 0) // position = 1st column
                {
                    directions.Enqueue(new Vector2(0, -1)); // GoDown
                }
                else if (snakeHead.X > 0 && snakeHead.Y == gameArray.GetLength(1)) // position = top line
                {
                    directions.Enqueue(new Vector2(-1, 0)); // GoLeft
                }
                else if (snakeHead.Y <= gameArray.GetLength(1)) // position = last column
                {
                    directions.Enqueue(new Vector2(1, 0)); // GoRight
                }
            }
            else
            {
                if (snakeHead.X == gameArray.GetLength(0) - 1) // position = last column
                {
                    loopFinish = true;
                    directions.Enqueue(new Vector2(0, 1)); // GoUp
                }
                else if (snakeHead.Y < gameArray.GetLength(1) - 1 && snakeHead.X % 2 != 0) // position column = odd and line < gridSize.Y - 1
                {
                    directions.Enqueue(new Vector2(0, 1)); // GoUp
                }
                else if (snakeHead.Y == gameArray.GetLength(1) - 1 && snakeHead.X % 2 != 0 && loopFinish == false) // position column = odd and line = gridSize.Y - 1
                {
                    directions.Enqueue(new Vector2(1, 0)); // GoRight
                }
                else if (snakeHead.Y >= 1 && snakeHead.X % 2 == 0) // position column = even and line >= bottom line
                {
                    directions.Enqueue(new Vector2(0, -1)); // GoDown
                }
                else if (snakeHead.Y == 0 && snakeHead.X % 2 == 0 && loopFinish == false)// position column = even and bottom line
                {
                    directions.Enqueue(new Vector2(1, 0)); // GoRight
                }
            }

            return directions;
        }

        private static void ExtractPositions(dynamic args)
        {
            //PARSE SERVER INFO
            string mapSizeStr = args.MapSize;
            string yourPositionStr = args.YourPosition;
            string foodPositionStr = args.FoodPosition;
            string otherSnakePositionStr = args.OtherSnakePosition;

            //EXTRACT GRID SIZE
            string[] mapSizeParts = mapSizeStr.Split('x');
            Vector2 gridSize = new(int.Parse(mapSizeParts[0].Trim()), int.Parse(mapSizeParts[1].Trim()));

            //EXTRACT SNAKE POSITION
            snakePositions = ParsePositions(yourPositionStr);
            List<Vector2> foodPositions = ParsePositions(foodPositionStr);
            List<Vector2> otherSnakePositions = ParsePositions(otherSnakePositionStr);

            //INITIALIZE MAP
            gameArray = InitializeGameArray(gridSize, snakePositions, foodPositions, otherSnakePositions);

            //PRINT MAP
            PrintGameArray(gameArray);
        }

        private static List<Vector2> ParsePositions(string positionsStr)
        {
            List<Vector2> positions = [];
            string[] positionsArray = positionsStr.Split(new string[] { "), (" }, StringSplitOptions.None);

            foreach (string pos in positionsArray)
            {
                try
                {
                    string cleanedPos = pos.Replace("(", "").Replace(")", "").Trim();
                    string[] coords = cleanedPos.Split(',');
                    if (coords.Length != 2)
                    {
                        Console.WriteLine($"Invalid coord format: {pos}");
                        continue; // Skip this iteration if the format is invalid
                    }

                    int x = int.Parse(coords[0].Trim());
                    int y = int.Parse(coords[1].Trim());

                    Vector2 position = new(x, y);
                    positions.Add(position);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing position '{pos}': {ex.Message}");
                }
            }
            return positions;
        }

        private static char[,] InitializeGameArray(Vector2 gridSize, List<Vector2> snakePositions, List<Vector2> foodPositions, List<Vector2> otherSnakePositions)
        {
            try
            {
                char[,] gameArray = new char[(int)gridSize.X, (int)gridSize.Y];

            // Initialize the game array with 'O' for empty spaces
            for (int x = 0; x < gridSize.X; x++)
            {
                for (int y = 0; y < gridSize.Y; y++)
                {
                    gameArray[x, y] = 'O';
                }
            }

            // Place the other snakes on the grid
            foreach (var pos in otherSnakePositions)
            {
                gameArray[(int)pos.X, (int)pos.Y] = 'E';
            }

            // Place the snake on the grid
            foreach (var pos in snakePositions)
            {
                gameArray[(int)pos.X, (int)pos.Y] = 'S';
            }

            // Place the food on the grid
            foreach (var pos in foodPositions)
            {
                gameArray[(int)pos.X, (int)pos.Y] = 'F';
            }

            return gameArray;
            }
            catch (Exception)
            {
                Console.WriteLine("Error while creating gameArray");
            }
            
            return null;
        }

        private static void PrintGameArray(char[,] gameArray)
        {
            Console.WriteLine("GAME ARRAY");
            try
            {
                for (int y = gameArray.GetLength(1)- 1; y >= 0; y--)
                {
                    for (int x = 0; x < gameArray.GetLength(0); x++)
                    {
                        Console.Write(gameArray[x, y]);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("\n");
            }
            catch(Exception e)
            {
                Console.WriteLine("Error in printing array : " + e);
            }
        }
    }
}