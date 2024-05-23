using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Http;
using Newtonsoft.Json;

namespace Tailed.ProgrammerGames.TicTacToe
{
    public interface IGameManager
    {
        void ProcessRPCMessage(string json);
    }

    public class ClientRPC
    {
        private static TcpClient client;
        private static NetworkStream stream;
        private static IGameManager gameManager = new GameManager();

        public static async Task ConnectToServerAsync(string hostname, int port)
        {
            try
            {
                Console.WriteLine("Launching...");
                client = new TcpClient();
                await client.ConnectAsync(hostname, port);
                stream = client.GetStream();
                ListenForResponsesAsync();
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
                    else 
                    {
                        throw new Exception("Server closed the connection.");
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
                string method = message.Method;
                var args = message.Args;

                switch(method)
                {
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
                    case "CommandList":
                        args = message.Args;
                        Console.WriteLine($"[CommandList]\n{args}\n");
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

        public static void RPCSendMessage(string method, object args = null)
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
            if(stream == null || !client.Connected)
            {
                Console.WriteLine("Failed : Not connected to server");
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
            stream.Write(data, 0, data.Length);
        }

        private static void Disconnect()
        {
            stream?.Close();
            client?.Close();
            Environment.Exit(1);
        }

        public static async Task Main(string[] args)
        {
            //CONNECTION TO SERVER 
            await ConnectToServerAsync("localhost",25001);
            
            //FOR COMMANDS LIST, TYPE :
            //RPCSendMessage("Help");
            
            await Task.Delay(-1);
        }
    }

    public class GameManager : IGameManager
    {
        private static int[,] gameBoard = new int[3,3];
        //EXEMPLE
        private static Random random = new(); //DELETE THIS
        private static int lastX = -1;
        private static int lastY = -1;
        //

        public void ProcessRPCMessage(string json)
        {
            var message = JsonConvert.DeserializeObject<dynamic>(json);
            string method = message.Method;
            var args = message.Args;

            switch(method)
            {
                case "Action":
                    HandleAction(args);
                    break;
                case "Data":
                    HandleData(args);
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

            //ACTION LOGIC HERE
            int x, y;

            do{
                x = random.Next(0, 3);
                y = random.Next(0, 3);
            }while(x == lastX && y == lastY);

            lastX= x;
            lastY= y;

            //SEND ACTION HERE
            ClientRPC.RPCSendMessage("PutToken", new { x, y});
            //

        }

        private static void HandleData(dynamic args)
        {
            //DISPLAY DATA TEXT
            Console.WriteLine($"[Data]\n{args}\n");

            //INSERT DATA HANDLING HERE
            
            //EXEMPLE
            string jsonArray = args.Array;
            int[][] tempArray = JsonConvert.DeserializeObject<int[][]>(jsonArray);

            for(int i = 0; i < 3; i++)
            {
                for(int j = 0; j < 3; j++)
                {
                    gameBoard[i, j] = tempArray[i][j];
                }
            }
            
            //DEBUG
            string finalArray = JsonConvert.SerializeObject(gameBoard);
            Console.WriteLine($"\n[ConvertedData]\n{finalArray}\n");
        }
    }
}