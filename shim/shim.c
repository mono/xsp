#include <errno.h>
#include <unistd.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <sys/socket.h>
#include <sys/un.h>

#define bool char
#define false 0
#define true 1

#define UNIX_PATH_MAX 108

bool start_server (const char * path, int * socket_fd)
{
    *socket_fd = socket(AF_UNIX, SOCK_STREAM, 0);
    if (*socket_fd == -1) {
        perror ("socket");
        return false;
    }

    struct sockaddr_un local;
    local.sun_family = AF_UNIX;

    if (strlen(path) >= UNIX_PATH_MAX) {
        fprintf(stderr, "Path %s is too long!", path);
        return false;
    }
    strcpy (local.sun_path, path);
    if (unlink (path) == -1 && errno != ENOENT) {
        perror ("unlink");
        return false;
    }

    socklen_t local_len = strlen (path) + sizeof (local.sun_family);
    if (bind (*socket_fd, (struct sockaddr *)&local, local_len) == -1) {
        perror ("bind");
        return false;
    }

    const int backlog = 5;
    if (listen (*socket_fd, backlog) == -1) {
        perror ("listen");
        return false;
    }

    return true;
}

bool spawn (char ** command)
{
    if (fork ())
        return true;

    printf("Spawning!\n");

    if (!execv (command[0], command + 1)) {
        perror("execv");
        exit (1);
    }

    return false;
}

bool run_connection (int fd, char ** command)
{
    for (;;) {
        char buffer[100];
        ssize_t received = recv (fd, buffer, 100, 0);

        if (received < 0) {
            perror ("recv");
            return false;
        }

        if (received == 0)
            return true;


        if (strcmp (buffer, "SPAWN\n") == 0) {
            if (send (fd, buffer, received, 0) < 0) {
                perror ("send");
                return false;
            }
            if (spawn (command)) {
                strcpy(buffer, "OK\n");
                if (send (fd, buffer, received, 0) < 0) {
                    perror ("send");
                    return false;
                }
            } else {
                strcpy(buffer, "NOK\n");
                if (send (fd, buffer, received, 0) < 0) {
                    perror ("send");
                    return false;
                }
            }
        } else {
            strcpy(buffer, "NACK!\n");
            if (send (fd, buffer, received, 0) < 0) {
                perror ("send");
                return false;
            }
        }
    }
}

int main (int argc, char * argv [])
{
    int local_fd;

    if (argc <= 2) {
        fprintf (stderr, "Usage: %s <socket> <command>\n", argv [0]);
        return 1;
    }

    const char * path = argv [1];
    char ** command = argv + 2;

    if (!start_server(path, &local_fd))
        return 1;

    for (;;) {
        printf ("Waiting for a connection...\n");
        struct sockaddr_un remote;
        socklen_t t = sizeof (remote);
        int remote_fd = accept (local_fd, (struct sockaddr *)&remote, &t);
        if (remote_fd == -1) {
            perror ("accept");
            return 1;
        }

        printf ("Connected.\n");

        if(!run_connection (remote_fd, command)) {
            printf ("Something went wrong while processing input\n");
        }

        close (remote_fd);
    }
}
