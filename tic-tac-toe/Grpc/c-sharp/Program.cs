using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using TTTGames;

class Program
{
    private static int[,] board = new int[3, 3];

    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting client");
        using var channel = GrpcChannel.ForAddress("http://localhost:25002");
        var invoker = channel.Intercept(m =>
        {
            m.Add("client-id", "UUID goes here");
            return m;
        });

        var client = new TTTGameService.TTTGameServiceClient(invoker);
        Console.WriteLine("Client started");
        try
        {
            Console.WriteLine("Attempting to connect");
            var connectReq = new ConnectRequest { ClientId = "Felix2" };
            var connectResponse = client.Connect(connectReq);
            Console.WriteLine($"Connection Message: '{connectResponse.Message}'");

            var playerTurnRequest = new PlayerTurnRequest();
            var emptyRequest = new EmptyRequest();

            using var playerTurnCall = client.PlayerTurn(playerTurnRequest);
            using var eventCall = client.Event(emptyRequest);

            var eventTask = HandleEventResponseAsync(eventCall.ResponseStream);
            var playerTurnTask = HandlePlayerTurnResponseAsync(playerTurnCall.ResponseStream, client);

            await Task.WhenAll(eventTask, playerTurnTask);

        }
        catch (RpcException e)
        {
            Console.WriteLine($"RPC Error: {e.Status}");
        }
    }

    private static async Task PutTokensAsync(TTTGameService.TTTGameServiceClient client)
    {
        Console.WriteLine("put token started");
        var availablePositions = new List<(int X, int Y)>();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == 0)
                {
                    availablePositions.Add((i, j));
                }
            }
        }

        var random = new Random();
        var (xPos, yPos) = availablePositions[random.Next(availablePositions.Count)];

        var putTokenRequest = new PutTokenRequest
        {
            X = xPos,
            Y = yPos
        };

        await client.PutTokenAsync(putTokenRequest);
    }

    private static async Task HandlePlayerTurnResponseAsync(IAsyncStreamReader<PlayerTurnResponse> responseStream, TTTGameService.TTTGameServiceClient client)
    {
        Console.WriteLine("Listening for player turn responses");
        await foreach (var playerTurnResponse in responseStream.ReadAllAsync())
        {
            Console.WriteLine("Received player turn response");

            Console.WriteLine("Board:");
            foreach (Coordinates coordinates in playerTurnResponse.Board)
            {
                int x = coordinates.X;
                int y = coordinates.Y;
                board[x, y] = coordinates.Value;
            }

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Console.Write($"{board[i, j]} ");
                    if ((j + 1) % 3 == 0)
                    {
                        Console.WriteLine();
                    }
                }
            }

            Console.WriteLine("");

            
            await PutTokensAsync(client);
            
            Console.WriteLine("End of PlayerTurn Loop - Listeting for new response stream");
        }
        Console.WriteLine("END END END  - Closing the stream listening loop");
    }

    private static async Task HandleEventResponseAsync(IAsyncStreamReader<EventMessage> responseStream)
    {
        Console.WriteLine("Listening for event messages");
        await foreach (var eventMessage in responseStream.ReadAllAsync())
        {
            Console.WriteLine($"Event Message: '{eventMessage.Message}'");
        }
    }
}