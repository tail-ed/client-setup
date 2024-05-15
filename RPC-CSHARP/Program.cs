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

                //DEBUG
                Console.WriteLine("RECEIVED : " + response);
            

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
            //OBJECTIF : LE USER CODE DANS LE MAIN, EN UTILISANT 
            //CLIENTRPC COMME PACKAGE
            //LE PROGRAM.CS DOIT SEULEMENT CONTENIR LE MAIN()

            //ERREURS POSSIBLE : 
            
            //RPCSendMessage("PutTok",2); --> does nothing
            //RPCSendMessage("PutToken", 2); --> does nothing
            //AJOUTER UNE VALIDATION DANS UNITY DIRECTEMENT POUR INFORMER
            //L'UTILISATEUR QU'UNE ERREUR EST SURVENUE LORS DE L'APPEL DE FONCTION


            try{
                //THROW ERROR :
                RPCSendMessage("",1);
                RPCSendMessage("PutToken",null);

                //EXEMPLE FONCTIONNEL
                RPCSendMessage("PutToken", new { x = 0, y = 0});
                //RPCSendMessage("PutToken", new { x = 2, y = 0}); //BOT TURN
                RPCSendMessage("PutToken", new { x = 1, y = 1});
                //RPCSendMessage("PutToken", new { x = 2, y = 1}); //BOT TURN
                RPCSendMessage("PutToken", new { x = 2, y = 2});
                RPCSendMessage("PutToken", new { obj = new System.IO.MemoryStream() }); //NON CONVERTIBLE EN JSON
            }
            catch(Exception e)
            {
                Console.Error.Write("MAIN ERROR : " + e.ToString());
            }
        }
    }
}