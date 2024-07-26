#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <stdbool.h>

#include <sys/socket.h>
#include <arpa/inet.h>

#include "cJSON.h"

#define EMPTY 0
#define PL_X 1
#define PL_O 2

#define SOCKET_ADDRESS "148.113.158.63"
#define SOCKET_PORT 25001

#define PLAYER_UUID_COMMAND "{\"Method\":\"Login\",\"Args\":{\"UUID\":\"669a8bbde0c9ffb3c8cb228c\"}}"
#define PUTTOKEN_COMMAND "{\"Method\":\"PutToken\",\"Args\":{\"x\": ,\"y\": }}"
#define PUTTOKEN_X_INDEX 33
#define PUTTOKEN_Y_INDEX 39

typedef struct {
    int board[3][3];
} Game;

bool haveToPlay;
Game board;
int socket_desc;

void processMessageSingleline(char *message) {

    cJSON *json = cJSON_Parse(message);
    cJSON *method = cJSON_GetObjectItem(json, "Method");
    cJSON *args = cJSON_GetObjectItem(json, "Args");

    if (!json || !method || !args) {
        return;
    }

    if (strcmp(method->valuestring, "Action") != 0) {
        return;
    }

    char *turn = cJSON_GetObjectItem(args, "Turn")->valuestring;
    char *array = cJSON_GetObjectItem(args, "Array")->valuestring;

    int xPtr = 0;
    int yPtr = 0;

    memset(&board, EMPTY, sizeof(board));
    for (char *c = array; *c != '\0'; c++) {
        if (*c >= '0' && *c <= '2') {
            board.board[xPtr][yPtr] = (int)(*c - '0');
            yPtr++;
            if (yPtr > 2) {
                yPtr = 0;
                xPtr++;
            }
        }
    }

    if (strcmp(turn, "Player") == 0) {
        haveToPlay = true;
    }

    cJSON_Delete(json);
}

void processMessageMultiline(char *message) {
    char toExecute[1024];
    char *start = message;

    for (char *c = message; *c != '\0'; c++) {
        if (*c == '\n') {
            memcpy(toExecute, start, c - start + 1); // Pretty dangerous considering toExecute has a fixed 1KiB size
            toExecute[c - start] = '\0';
            processMessageSingleline(toExecute);
            start = c + 1;
        } else if (*(c + 1) == '\0') {
            memcpy(toExecute, start, c - start + 1);
            processMessageSingleline(toExecute);
        }
    }
}

char *getAction() {

    // Decision algorithm
    int row = -1;
    int col = -1;

    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            if (board.board[i][j] == EMPTY) {
                row = i;
                col = j;
            }
        }
    }

    char *action = malloc(sizeof(char) * 1024);
    if (!action) return NULL;

    strcpy(action, PUTTOKEN_COMMAND);

    action[PUTTOKEN_X_INDEX] = row + '0';
    action[PUTTOKEN_Y_INDEX] = col + '0';

    return action;
}

int main(int charc, char **argv) {
    memset(&board, EMPTY, sizeof(board));
    haveToPlay = false;

    socket_desc = socket(AF_INET, SOCK_STREAM, 0);

    if (socket_desc < 0) {
        printf("Unable to create socket\n");
        return 1;
    }

    struct sockaddr_in server_addr;
    server_addr.sin_family = AF_INET;
    server_addr.sin_port = htons(SOCKET_PORT);
    server_addr.sin_addr.s_addr = inet_addr(SOCKET_ADDRESS);

    if (connect(socket_desc, (struct sockaddr *)&server_addr, sizeof(server_addr)) < 0) {
        printf("Unable to connect to game server\n");
        close(socket_desc);
        return 1;
    }

    printf("Connected to server\n");

    // Send UUID
    const char *uuid_message = PLAYER_UUID_COMMAND;
    if (send(socket_desc, uuid_message, strlen(uuid_message), 0) < 0) {
        printf("Unable to send message\n");
        close(socket_desc);
        return 1;
    }

    for (;;) {

        char server_message[1024];
        memset(server_message, 0, sizeof(server_message));

        if (recv(socket_desc, server_message, 1024, 0) < 0) {
            printf("Unable to receive message\n");
            close(socket_desc);
            return 1;
        }

        if (strlen(server_message) > 0) {
            printf("Start server response: %s\n", server_message);
            printf("End server response\n");
        }
        
        processMessageMultiline(server_message);

        if (!haveToPlay) continue;

        // If have to play, send action
        char *client_message_action = getAction();

        if (send(socket_desc, client_message_action, strlen(client_message_action), 0) < 0) {
            printf("Unable to send message\n");
            close(socket_desc);
            return 1;
        }

        printf("Start message sent: %s\n", client_message_action);
        printf("End message sent\n");
        free(client_message_action);
        haveToPlay = false;
    }

    close(socket_desc);

    return 0;
}