# Tic-tac-toe RPC game client in C

## Compilation

```bash
make release
```

## Execution

```bash
./bin/socket
```

## Dependencies

This client uses the POSIX-compliant libraries `sys/socket.h` and `arpa/inet.h`, which are generally distributed with C compilers for UNIX systems like `gcc`.

To run this client on a non-POSIX compliant OS (such as Windows), one would have to use specific OS libraries instead (for example, `winsock2.h` for Windows)
