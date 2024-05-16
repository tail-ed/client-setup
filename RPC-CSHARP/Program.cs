using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace Tailed.ProgrammerGames.TicTacToe
{
    public class ClientRPC
    {
        public static void RPCSendMessage(string method, object args)
        {
            
                //INPUT VALIDATION - CHECK IF NULL OR EMPTY
                if(string.IsNullOrEmpty(method))
                {
                    throw new ArgumentException("Method name cannot be null or empty --> ", nameof(method));
                }

                if(args == null)
                {
                    throw new ArgumentException("Object cannot be null or empty --> ", nameof(args));
                }

                //CREATE JSON-RPC MESSAGE
                var rpcMessage = new { method = method, args = args };

                //CONVERT MESSAGE TO JSON
                string jsonMessage = JsonConvert.SerializeObject(rpcMessage);
                
                //SEND MESSAGE AND RECEIVE RESPONSE
                string response = SendAndReceiveMessage(jsonMessage);

                //SERVER FEEDBACK
                Console.WriteLine("SERVER RESPONSE : " + response);
            

        }

        public static void RPCSendMessage(string method)
        {
            
                //INPUT VALIDATION - CHECK IF NULL OR EMPTY
                if(string.IsNullOrEmpty(method))
                {
                    throw new ArgumentException("Method name cannot be null or empty --> ", nameof(method));
                }

                //CREATE JSON-RPC MESSAGE
                var rpcMessage = new { method = method};

                //CONVERT MESSAGE TO JSON
                string jsonMessage = JsonConvert.SerializeObject(rpcMessage);
                
                //SEND MESSAGE AND RECEIVE RESPONSE
                string response = SendAndReceiveMessage(jsonMessage);

                //SERVER FEEDBACK
                Console.WriteLine("SERVER RESPONSE : " + response);
            

        }

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


        public static void Main(string[] args)
        {
            try{

                //FOR COMMANDS LIST, TYPE :
                RPCSendMessage("Help");

                RPCSendMessage("PutToken", new {x = 2, y = 2});
                RPCSendMessage("GetBoard"); // --> Doesn't return a valid value
                RPCSendMessage("CheckForVictory");

                //ERRORS HANDLED IN RPC :
                //RPCSendMessage("");
                //RPCSendMessage("PutToken",null);
                //RPCSendMessage("PutToken", new { obj = new System.IO.MemoryStream() }); //NON CONVERTIBLE EN JSON

            }
            catch(Exception e)
            {
                Console.Error.Write("MAIN ERROR : " + e.ToString());
            }
        }
    }
}