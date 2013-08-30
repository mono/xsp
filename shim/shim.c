#include <assert.h>
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

#define BUFFER_SIZE 100

bool start_server (const char * path, int * socket_fd)
{
    assert (path != 0);
    assert (socket_fd != 0);
    *socket_fd = socket (AF_UNIX, SOCK_STREAM, 0);
    if (*socket_fd == -1) {
        perror ("socket");
        return false;
    }

    struct sockaddr_un local;
    local.sun_family = AF_UNIX;

    if (strlen (path) >= UNIX_PATH_MAX) {
        fprintf (stderr, "Path %s is too long!", path);
        return false;
    }
    strncpy (local.sun_path, path, UNIX_PATH_MAX - 1);
    local.sun_path [UNIX_PATH_MAX - 1] = 0;
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

pid_t spawn (char * command)
{
    assert (command != 0);
    pid_t child = fork ();
    if (child)
        return child;

    printf ("Spawning!\n");

    char * args [4];
    args [0] = "/bin/sh";
    args [1] = "-c";
    args [2] = command;
    args [3] = 0;

    if (execv (*args, args) == -1) {
        perror ("execv");
        exit (1);
    }

    return -1;
}

bool run_connection (int fd, char * command)
{
    assert (fd != 0);
    assert (command != 0);
    for (;;) {
        char buffer [BUFFER_SIZE];
        ssize_t received = recv (fd, buffer, BUFFER_SIZE, 0);

        if (received < 0) {
            perror ("recv");
            return false;
        }

        if (received == 0)
            return true;


        if (strncmp (buffer, "SPAWN\n", 6) == 0) {
            pid_t spawned = spawn (command);
            if (spawned >= 0) {
                int written = snprintf (buffer, BUFFER_SIZE - 1, "%d\n", spawned);
                buffer [written] = 0;
                printf ("Shim: Sending %d\n", spawned);
                if (send (fd, buffer, received, 0) < 0) {
                    perror ("send");
                    return false;
                }
            } else {
                strncpy (buffer, "NOK\n", 4);
                buffer [5] = 0;
                printf ("Shim: Sending NOK\n");
                if (send (fd, buffer, received, 0) < 0) {
                    perror ("send");
                    return false;
                }
            }
        } else {
            strncpy (buffer, "NACK!\n", 6);
            buffer [7] = 0;
            printf ("Shim: Sending NACK!\n");
            if (send (fd, buffer, received, 0) < 0) {
                perror ("send");
                return false;
            }
        }
    }
}

int main (int argc, char * argv [])
{
	printf ("Shim: I'm uid %d euid %d\n", getuid (), geteuid ());
	uid_t euid = geteuid ();
	setreuid (euid, euid);
	printf ("Shim: I'm uid %d euid %d\n", getuid (), geteuid ());
    int local_fd;

    if (argc <= 2) {
        fprintf (stderr, "Usage: %s <socket> <command>\n", argv [0]);
        return 1;
    }

    const char * path = argv [1];

    int total_length = 0;

    int i;
    for (i = 2; i < argc; i++)
        total_length += strlen (argv [i]) + 1;

    char * command = malloc (total_length * sizeof (char));
    if (!command) {
        perror ("malloc");
        return 1;
    }
    int j = 0;
    for (i = 2; i < argc; i++) {
        strcpy(command + j, argv [i]);
        j += strlen (argv [i]) + 1;
        command [j - 1] = ' ';
    }
    command [total_length - 1] = 0;

    printf ("Shim: Will run %s\n", command);

    if (!start_server (path, &local_fd))
        return 1;

    for (;;) {
        printf ("Shim: Waiting for a connection...\n");
        struct sockaddr_un remote;
        socklen_t t = sizeof (remote);
        int remote_fd = accept (local_fd, (struct sockaddr *)&remote, &t);
        if (remote_fd == -1) {
            perror ("accept");
            return 1;
        }

        printf ("Shim: Connected.\n");

        if (!run_connection (remote_fd, command)) {
            printf ("Shim: Something went wrong while processing input\n");
        }

        close (remote_fd);
    }
}
