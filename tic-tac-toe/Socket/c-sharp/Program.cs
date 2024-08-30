using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace Tailed.ProgrammerGames.TicTacToe
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
            }

            //CONNECTION TO SERVER
            await ConnectToServerAsync("socket.tictactoe.tailed.ca",25001);
    
            //FOR COMMANDS LIST, TYPE :
            //RPCSendMessage("Help");
            
            await Task.Delay(-1);
        }
    }

    //GAMEPLAY EXEMPLE : (BASIC)
    public class GameManager : IGameManager
    {
        private static int[,] gameBoard = new int[3,3];
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
            //DISPLAY ACTION TEXT
            Console.WriteLine($"[Action]\n{args}\n");

            //GET GAME ARRAY 
            string jsonArray = args.Array;
            gameBoard = JsonConvert.DeserializeObject<int[,]>(jsonArray);

            //ACTION LOGIC HERE
            //EXEMPLE : PICK A RANDOM AVAILABLE SPOT
            int x, y;
            
            do
            {
                x = random.Next(0, 3);
                y = random.Next(0, 3);
            }
            while(gameBoard[x,y] != 0);
            
            //SEND ACTION HERE
            ClientRPC.RPCSendMessage("PutToken", new { x, y});
            Console.WriteLine("Sending PutToken");
            //
        }
    }
}