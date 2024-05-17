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


        /*
        private static string SendAndReceiveMessage(string jsonMessage)
        {
            //CONNECTION TO SERVER
            using var client = new TcpClient("localhost", 25001);
            using var stream = client.GetStream();

            //SEND JSON MESSAGE
            byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
            stream.Write(data, 0, data.Length);

            //RECEIVE RESPONSE
            return ReceiveResponse(stream);
        }

        private static string ReceiveResponse(NetworkStream stream)
        {
            //RECEIVE RESPONSE
            byte[] buffer = new byte[1024];
            StringBuilder response = new();
            
            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                response.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            }while(bytesRead == buffer.Length);
            
            return response.ToString();
        }
        */


        public static async Task Main(string[] args)
        {
            //CONNECTION TO SERVER 
            await ConnectToServerAsync("localhost",25001);
            
            //FOR COMMANDS LIST, TYPE :
            //RPCSendMessage("Help");

            //INSERT CODE HERE :
                RPCSendMessage("Help");
                //RPCSendMessage("GetBoard");
                //RPCSendMessage("PutToken", new {x = 2, y = 2});
                //RPCSendMessage("PutToken", new {x = 1, y = 2});

                //ERRORS HANDLED IN RPC :
                //RPCSendMessage("");
                //RPCSendMessage("PutToken",null);
                //RPCSendMessage("PutToken", new { obj = new System.IO.MemoryStream() }); //NON CONVERTIBLE EN JSON
            
            //
            await Task.Delay(-1);
        }
    }
}