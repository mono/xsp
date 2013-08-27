#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <string.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#define bool char
#define false 0
#define true 1

#define UNIX_PATH_MAX 108

int main (int argc, char * argv [])
{
    if (argc <= 1) {
        fprintf (stderr, "Usage: %s <socket>\n", argv [0]);
        return 1;
    }

    const char * path = argv [1];

    int remote_fd = socket (AF_UNIX, SOCK_STREAM, 0);
    if (remote_fd == -1) {
        perror ("socket");
        return 1;
    }

    printf ("Trying to connect...\n");

    struct sockaddr_un remote;
    remote.sun_family = AF_UNIX;
    if (strlen(path) >= UNIX_PATH_MAX) {
        fprintf(stderr, "Path %s is too long!", path);
        return false;
    }
    strcpy (remote.sun_path, path);
    socklen_t len = strlen (remote.sun_path) + sizeof(remote.sun_family);
    if (connect (remote_fd, (struct sockaddr *)&remote, len) == -1) {
        perror ("connect");
        return 1;
    }

    printf ("Connected.\n");

    char buffer [100];
    while (printf ("input> "), fgets (buffer, 100, stdin), !feof (stdin)) {
        if (send (remote_fd, buffer, strlen(buffer), 0) == -1) {
            perror ("send");
            return 1;
        }

        ssize_t received = recv (remote_fd, buffer, 100, 0);
        if (received > 0) {
            buffer[received] = '\0';
            printf ("echo > %s", buffer);
        } else {
            if (received < 0)
                perror("recv");
            else
                printf("Server closed connection\n");
            return 1;
        }
    }

    close(remote_fd);

    return 0;
}
