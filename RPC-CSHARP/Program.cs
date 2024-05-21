using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Tailed.ProgrammerGames.TicTacToe
{
    public class ClientRPC
    {
        private static TcpClient client;
        private static NetworkStream stream;

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
            try
            {
                byte[] buffer = new byte[1024];
                while(true)
                {
                    var bytesReceived = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if(bytesReceived > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        Console.WriteLine("SERVER RESPONSE : " + response);
                    }
                    else 
                    {
                        throw new Exception("Server closed the connection.");
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error receiving data from server : " + e.Message);
                Disconect();
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

        private static void Disconect()
        {
            stream?.Close();
            client?.Close();
        }

        public static async Task Main(string[] args)
        {
            //CONNECTION TO SERVER 
            await ConnectToServerAsync("localhost",25001);
            
            //FOR COMMANDS LIST, TYPE :
            //RPCSendMessage("Help");

            //INSERT CODE HERE :
                //RPCSendMessage("Help");
                //RPCSendMessage("PlayerTurn");

                //RPCSendMessage("PutToken", new {x = 4, y = 2});
                //RPCSendMessage("PutToken", new {x = false, y = true});

                //RPCSendMessage("PutToken", new {x = 'W', y = 'S'});
                RPCSendMessage("PutToken", new {x = 2, y = 2});
                //RPCSendMessage("PutToken", new {x = 1, y = 2});

                //RPCSendMessage("PutToken", new {x = 1, y = 2});
                //RPCSendMessage("GetBoard");

                //ERRORS HANDLED IN RPC :
                //RPCSendMessage("");
                //RPCSendMessage("PutToken",null);
                //RPCSendMessage("PutToken", new { obj = new System.IO.MemoryStream() }); //NON CONVERTIBLE EN JSON
            
            //
            await Task.Delay(-1);
        }
    }
}