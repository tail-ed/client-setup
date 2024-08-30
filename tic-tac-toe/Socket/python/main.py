import socket
import json
import sys
import threading
import random

class GameManager:
    game_board = [[0 for _ in range(3)] for _ in range(3)]

    @staticmethod
    def process_rpc_message(json_message):
        message = json.loads(json_message)
        if not message:
            return

        method = message.get('Method')
        args = message.get('Args')

        if method == 'Action':
            GameManager.handle_action(args)
        else:
            print(f"Unhandled message type: {method}")

    @staticmethod
    def handle_action(args):
        print(f"[Action]\n{json.dumps(args, indent=2)}\n")

        json_array = args['Array']
        GameManager.game_board = json.loads(json_array)

        x, y = 0, 0
        while GameManager.game_board[x][y] != 0:
            x = random.randint(0, 2)
            y = random.randint(0, 2)

        ClientRPC.rpc_send_message('PutToken', {'x': x, 'y': y})
        print("Sending PutToken")

class ClientRPC:
    client = None
    stream = None
    UUID = ''  # ****************************************

    @staticmethod
    def connect_to_server(hostname, port):
        try:
            ClientRPC.client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            ClientRPC.client.connect((hostname, port))
            print('Connected to server')
            ClientRPC.listen_for_responses()
        except Exception as ex:
            print('Failed to connect: ' + str(ex))

    @staticmethod
    def listen_for_responses():
        response_builder = ''
        while True:
            try:
                data = ClientRPC.client.recv(4096)
                if not data:
                    break
                response = data.decode('utf-8')
                response_builder += response

                messages = response_builder.split('\n')
                for i in range(len(messages) - 1):
                    message = messages[i].strip()
                    if message:
                        print(f"Received: {message}")
                        ClientRPC.process_message(message)

                response_builder = messages[-1]
            except Exception as err:
                print('Error receiving data from server: ' + str(err))
                ClientRPC.disconnect()
                break

    @staticmethod
    def process_message(json_message):
        try:
            message = json.loads(json_message)
            if not message:
                return
            method = message.get('Method')
            args = message.get('Args')

            if method == 'Login':
                print(f"[Login]\n{json.dumps(args, indent=2)}\n")
                ClientRPC.rpc_send_message('Login', {'UUID': ClientRPC.UUID})
            elif method == 'Event':
                if args.get('MethodName') == 'ServerClosing':
                    print(f"[Event]\n{json.dumps(args, indent=2)}\n")
                    ClientRPC.disconnect()
                else:
                    print(f"[Event]\n{json.dumps(args, indent=2)}\n")
            elif method == 'Help':
                print(f"[Help]\n{json.dumps(args, indent=2)}\n")
            else:
                GameManager.process_rpc_message(json_message)
        except Exception as ex:
            print('Error processing message: ' + str(ex))

    @staticmethod
    def rpc_send_message(method, args=None):
        try:
            if not method:
                raise ValueError('Method name cannot be null or empty')

            rpc_message = {'method': method, 'args': args}
            json_message = json.dumps(rpc_message)
            ClientRPC.send(json_message)
        except Exception as e:
            print('RPC MESSAGE ERROR: ' + str(e))

    @staticmethod
    def send(json_message):
        if not ClientRPC.client:
            print('Failed: Not connected to server')
            return

        data = json_message.encode('utf-8')
        ClientRPC.client.sendall(data)

    @staticmethod
    def disconnect():
        if ClientRPC.client:
            ClientRPC.client.close()
        sys.exit(1)

def main():
    if len(sys.argv) > 1:
        ClientRPC.UUID = sys.argv[1]
        print('Launching with UUID: ' + ClientRPC.UUID + '...')
    else:
        print('No UUID provided, closing...')
        sys.exit(1)

    ClientRPC.connect_to_server('socket.tictactoe.tailed.ca', 25001)

if __name__ == '__main__':
    main()
