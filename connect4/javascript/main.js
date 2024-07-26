const net = require('net');

class GameManager {
    static gameBoard = Array.from({ length: 3 }, () => Array(3).fill(0));
    static random = Math.random;

    static processRPCMessage(json) {
        const message = JSON.parse(json);
        if (!message) return;

        const method = message.Method;
        const args = message.Args;

        switch (method) {
            case 'Action':
                this.handleAction(args);
                break;
            default:
                console.log(`Unhandled message type: ${method}`);
                break;
        }
    }

    static handleAction(args) {
        console.log(`[Action]\n${JSON.stringify(args)}\n`);

        const jsonArray = args.Array;
        this.gameBoard = JSON.parse(jsonArray);

        let x, y;
        do {
            x = Math.floor(this.random() * 3);
            y = Math.floor(this.random() * 3);
        } while (this.gameBoard[x][y] !== 0);

        ClientRPC.rpcSendMessage('PutToken', { x, y });
        console.log('Sending PutToken');
    }
}

class ClientRPC {
    static client = null;
    static stream = null;
    static UUID = ''; //****************************************

    static async connectToServer(hostname, port) {
        try {
            this.client = new net.Socket();
            this.client.connect(port, hostname, () => {
                console.log('Connected to server');
                this.stream = this.client;
                this.listenForResponses();
            });
        } catch (ex) {
            console.log('Failed to connect: ' + ex.message);
        }
    }

    static listenForResponses() {
        let responseBuilder = '';

        this.stream.on('data', (data) => {
            const response = data.toString('utf8');
            responseBuilder += response;

            const messages = responseBuilder.split('\n');
            for (let i = 0; i < messages.length - 1; i++) {
                const message = messages[i].trim();
                if (message) {
                    console.log(`Received: ${message}`);
                    this.processMessage(message);
                }
            }

            responseBuilder = messages[messages.length - 1];
        });

        this.stream.on('error', (err) => {
            console.log('Error receiving data from server: ' + err.message);
            this.disconnect();
        });

        this.stream.on('close', () => {
            this.disconnect();
        });
    }

    static processMessage(jsonMessage) {
        try {
            const message = JSON.parse(jsonMessage);
            if (!message) return;
            const method = message.Method;
            const args = message.Args;

            switch (method) {
                case 'Login':
                    console.log(`[Login]\n${JSON.stringify(args)}\n`);
                    this.rpcSendMessage('Login', { UUID: this.UUID });
                    break;
                case 'Event':
                    if (args.MethodName === 'ServerClosing') {
                        console.log(`[Event]\n${JSON.stringify(args)}\n`);
                        this.disconnect();
                    } else {
                        console.log(`[Event]\n${JSON.stringify(args)}\n`);
                    }
                    break;
                case 'Help':
                    console.log(`[Help]\n${JSON.stringify(args)}\n`);
                    break;
                default:
                    GameManager.processRPCMessage(jsonMessage);
                    break;
            }
        } catch (ex) {
            console.error('Error processing message: ' + ex.message);
        }
    }

    static rpcSendMessage(method, args = null) {
        try {
            if (!method) {
                throw new Error('Method name cannot be null or empty');
            }

            const rpcMessage = { method, args };
            const jsonMessage = JSON.stringify(rpcMessage);
            this.send(jsonMessage);
        } catch (e) {
            console.error('RPC MESSAGE ERROR: ' + e.message);
        }
    }

    static send(jsonMessage) {
        if (!this.client || !this.stream) {
            console.log('Failed: Not connected to server');
            return;
        }

        const data = Buffer.from(jsonMessage, 'utf8');
        this.stream.write(data);
    }

    static disconnect() {
        if (this.stream) this.stream.end();
        if (this.client) this.client.end();
        process.exit(1);
    }
}

(async function main() {
    const args = process.argv.slice(2);
    if (args.length > 0) {
        const uuid = args[0];
        ClientRPC.UUID = uuid;
        console.log('Launching with UUID: ' + uuid + '...');
    } else {
        console.log('No UUID provided, closing...');
        process.exit(1);
    }

    await ClientRPC.connectToServer('socket.tictactoe.tailed.ca', 25001);

    process.stdin.resume();
})();
