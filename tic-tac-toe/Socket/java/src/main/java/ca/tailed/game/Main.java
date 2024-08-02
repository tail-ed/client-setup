package ca.tailed.game;

import com.google.gson.*;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.PrintWriter;
import java.net.Socket;
import java.util.Random;

class GameManager {
    private static int[][] gameBoard = new int[3][3];
    private static Random random = new Random();
    private static Gson gson = new Gson();

    public static void processRPCMessage(String json) {
        try {
            JsonObject message = JsonParser.parseString(json).getAsJsonObject();
            String method = message.get("Method").getAsString();
            JsonObject args = message.get("Args").getAsJsonObject();

            switch (method) {
                case "Action":
                    handleAction(args);
                    break;
                default:
                    System.out.println("Unhandled message type: " + method);
                    break;
            }
        } catch (JsonSyntaxException e) {
            System.out.println("Failed to parse JSON: " + e.getMessage());
        }
    }

    private static void handleAction(JsonObject args) {
        System.out.println("[Action]\n" + gson.toJson(args) + "\n");

        String jsonArray = args.get("Array").getAsString();
        JsonArray gameArray = JsonParser.parseString(jsonArray).getAsJsonArray();
        gameBoard = new int[gameArray.size()][];
        for (int i = 0; i < gameArray.size(); i++) {
            JsonArray row = gameArray.get(i).getAsJsonArray();
            gameBoard[i] = new int[row.size()];
            for (int j = 0; j < row.size(); j++) {
                gameBoard[i][j] = row.get(j).getAsInt();
            }
        }

        int x, y;
        do {
            x = random.nextInt(3);
            y = random.nextInt(3);
        } while (gameBoard[x][y] != 0);

        JsonObject putTokenArgs = new JsonObject();
        putTokenArgs.addProperty("x", x);
        putTokenArgs.addProperty("y", y);
        ClientRPC.rpcSendMessage("PutToken", putTokenArgs);
        System.out.println("Sending PutToken");
    }
}

class ClientRPC {
    private static Socket client = null;
    private static PrintWriter out = null;
    private static BufferedReader in = null;
    private static String UUID = ""; // ****************************************
    private static final Gson gson = new Gson();

    public static void connectToServer(String hostname, int port) {
        try {
            client = new Socket(hostname, port);
            out = new PrintWriter(client.getOutputStream(), true);
            in = new BufferedReader(new InputStreamReader(client.getInputStream()));
            System.out.println("Connected to server");
            listenForResponses();
        } catch (IOException ex) {
            System.out.println("Failed to connect: " + ex.getMessage());
        }
    }

    public static void listenForResponses() {
        new Thread(() -> {
            try {
                StringBuilder responseBuilder = new StringBuilder();
                String line;
                while ((line = in.readLine()) != null) {
                    String message = line.trim();
                    if (!message.isEmpty()) {
                        System.out.println("Received: " + message);
                        processMessage(message);
                    }
                }
            } catch (IOException e) {
                System.out.println("Error receiving data from server: " + e.getMessage());
                disconnect();
            }
        }).start();
    }

    public static void processMessage(String jsonMessage) {
        try {
            JsonObject message = JsonParser.parseString(jsonMessage).getAsJsonObject();
            String method = message.get("Method").getAsString();
            JsonObject args = message.get("Args").getAsJsonObject();

            switch (method) {
                case "Login":
                    System.out.println("[Login]\n" + gson.toJson(args) + "\n");
                    JsonObject loginArgs = new JsonObject();
                    loginArgs.addProperty("UUID", UUID);
                    rpcSendMessage("Login", loginArgs);
                    break;
                case "Event":
                    if (args.get("MethodName").getAsString().equals("ServerClosing")) {
                        System.out.println("[Event]\n" + gson.toJson(args) + "\n");
                        disconnect();
                    } else {
                        System.out.println("[Event]\n" + gson.toJson(args) + "\n");
                    }
                    break;
                case "Help":
                    System.out.println("[Help]\n" + gson.toJson(args) + "\n");
                    break;
                default:
                    GameManager.processRPCMessage(jsonMessage);
                    break;
            }
        } catch (JsonSyntaxException ex) {
            System.out.println("Error processing message: " + ex.getMessage());
        }
    }

    public static void rpcSendMessage(String method, JsonObject args) {
        try {
            if (method == null || method.isEmpty()) {
                throw new IllegalArgumentException("Method name cannot be null or empty");
            }

            JsonObject rpcMessage = new JsonObject();
            rpcMessage.addProperty("method", method);
            rpcMessage.add("args", args);
            String jsonMessage = gson.toJson(rpcMessage);
            send(jsonMessage);
        } catch (Exception e) {
            System.out.println("RPC MESSAGE ERROR: " + e.getMessage());
        }
    }

    private static void send(String jsonMessage) {
        if (client == null || out == null) {
            System.out.println("Failed: Not connected to server");
            return;
        }

        out.println(jsonMessage);
    }

    public static void disconnect() {
        try {
            if (in != null) in.close();
            if (out != null) out.close();
            if (client != null) client.close();
        } catch (IOException e) {
            e.printStackTrace();
        }
        System.exit(1);
    }

    public static void main(String[] args) {
        if (args.length > 0) {
            UUID = args[0];
            System.out.println("Launching with UUID: " + UUID + "...");
        } else {
            System.out.println("No UUID provided, closing...");
            System.exit(1);
        }

        connectToServer("socket.tictactoe.tailed.ca", 25001);
    }
}
