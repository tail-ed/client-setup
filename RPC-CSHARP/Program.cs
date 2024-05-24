using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;

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

        public static async Task ConnectToServerAsync(string hostname, int port)
        {
            try
            {
                Console.WriteLine("Launching...");
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
                if(message == null) return;
                string method = message.Method;
                var args = message.Args;

                switch(method)
                {
                    //IMPLEMENT METHODS HERE
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
            //CONNECTION TO SERVER 
            await ConnectToServerAsync("localhost",25001);
            
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

            private const int Empty = 0; //Empty space
            private const int Player_X = 1; //Client Token
            private const int Bot_O = 2; //Bot Token

        private static void HandleAction(dynamic args)
        {
            //DISPLAY ACTION TEXT
            Console.WriteLine($"[Action]\n{args}\n");

            //GET GAME ARRAY 
            string jsonArray = args.Array;
            gameBoard = JsonConvert.DeserializeObject<int[,]>(jsonArray);

            //ACTION LOGIC HERE
            //EXEMPLE : PICK A RANDOM AVAILABLE SPOT
            float x,y;

            x = GetBestMove(gameBoard).X;
            y = GetBestMove(gameBoard).Y;
            

            
            //SEND ACTION HERE
            ClientRPC.RPCSendMessage("PutToken", new { x , y});
            //
        }
        public static int MiniMax(int[,] board, int depth, bool isMaximizingPlayer, int alpha, int beta)
        {
            int score = EvaluateBoard(board);
            if (score != 0 || depth == 0) //if a position as a score
            {
                return score;
            }

            if (isMaximizingPlayer) //player turn
            {
                int bestscore = int.MinValue;
                for (int i = 0; i < board.GetLength(0); i++)
                {
                    for (int j = 0; j < board.GetLength(1); j++) //Get every cell of the board
                    {
                        if (board[i, j] == Empty)
                        {
                            board[i, j] = Player_X; // put a temporary token
                            int currentScore = MiniMax(board,depth-1, false, alpha, beta); //check the score of all possible position with the temporary token
                            board[i, j] = Empty; // delete the temporary token
                            if (currentScore > bestscore)
                            {
                                bestscore = currentScore;
                            }

                            if (currentScore > alpha)
                            {
                                alpha = currentScore;
                            }

                            if (beta <= alpha)
                            {
                                break;
                            }
                        }
                    }
                }
                return bestscore;
            }
            else // bot turn
            {
            int bestscore = int.MaxValue;
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++) // Get every cell of the board
                {
                    if (board[i, j] == Empty)
                    {
                        board[i, j] = Bot_O; // put a temporary token
                        int currentScore = MiniMax(board,depth-1, true, alpha, beta); //check the score of all possible position with the temporary token
                        board[i, j] = Empty; // delete the temporary token

                        if (currentScore < bestscore){
                            bestscore = currentScore;
                        }

                        if (beta < currentScore){
                            alpha = currentScore;
                        }

                        if (beta <= alpha)
                        {
                            break;
                        }
                    }
                }
            }
            return bestscore;
        }
        }
        
        public static int EvaluateBoard(int[,] board)
        {
            int score = 0; //Determine the spot with the highest score (highest score == priority to place a Token)
            //Check Rows
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    score += EvaluateLine(board[i, j], board[i, j], board[i, j]); // Check if the player can win in a rows
                }
            }

            //Check columns
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    score += EvaluateLine(board[i, j], board[i, j], board[i, j]); // Check if the player can win in a columns
                }
            }

            //Check Diagonal
            score += EvaluateLine(board[0, 0], board[1, 1], board[2, 2]); // Check if the player can win on a diagonal
            score += EvaluateLine(board[0, 2], board[1, 1], board[2, 0]); // Check if the player can win on a diagonal

            return score;
        }
        public static int EvaluateLine(int cell1, int cell2, int cell3)
        {
            int score = 0;
            if (cell1 == Player_X) { score = 1; } // + 1 score (priority higher)
            else if (cell1 == Bot_O) { score = -1; } // - 1 score (priority lower)

            if (cell2 == Player_X) { score *= 10; } // * 10 score ( Priority ++ higher (player can win)) 
            else if (cell2 == Bot_O) { score *= -10; }// * -10 score (Priority lower (no danger of losing))

            if (cell3 == Player_X)
            {
                if (score > 0) // if X
                {
                    score *= 10;
                }
                else if (score < 0) // if 0
                {
                    score = 0; //Block line for X
                }
                else //Empty
                {
                    score = 1;
                }
            }
            else if (cell3 == Bot_O)
            {
                if (score < 0) // if 0
                {
                    score *= 10;
                }
                else if (score > 1) // if X
                {
                    score = 0; //Block line for 0
                }
                else //Empty
                {
                    score = 1;
                }
            }
            return score;
        }
        public static Vector2 GetBestMove(int[,] board)
        {
            int bestScore = int.MinValue;
            Vector2 bestMove = new Vector2(0,0);

            for (int i=0; i< board.GetLength(0); i++)
            {
                for (int j=0; j< board.GetLength(1); j++)// Get every cell of the board
                {
                    if (board[i, j] == Empty)
                    {
                        board[i, j] = Player_X; // put a temporary token

                        int tempScore = EvaluateBoard(board); //Check the board if it's the best move
                        if (tempScore == 1) //if it is
                        {
                            bestMove = new Vector2(i,j); //return the best move
                            return bestMove;
                        }

                        int score = MiniMax(board, 1, true, int.MinValue, int.MaxValue); // if it can not block immediatly, check every possibility for this game
                        board[i, j] = Empty; // delete the temporary token

                        if (score > bestScore) //Keep the best place into the bestscore ---- if the score is equal, keep the first position
                        {
                            bestScore = score;
                            bestMove = new Vector2(i,j);
                        }
                    }
                }
            }
            return bestMove;
        }
    
    }
}