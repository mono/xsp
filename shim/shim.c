#include <assert.h>
#include <errno.h>
#include <unistd.h>
#include <stdio.h>
#include <string.h>
#include <stdarg.h>
#include <stdlib.h>
#include <time.h>
#include <sys/socket.h>
#include <sys/stat.h>
#include <sys/un.h>

#define bool char
#define false 0
#define true 1

#define UNIX_PATH_MAX 108

#define BUFFER_SIZE 10

#define PREFIX "%5d    shim     [%s]        : "

bool debug = false;
bool verbose = false;

char time_buffer[50];

char * time_str (void)
{
    time_t now = time (0);
    strftime (time_buffer, 50, "%Y-%m-%d %H:%M:%S       ", localtime (&now));
    return time_buffer;
}

void print_prefix (void)
{
	if (verbose)
		printf (PREFIX, getpid (), time_str ());
}

void log1 (char * str)
{
    if(!debug) return;
	print_prefix ();
    printf("%s\n", str);
}

void log(char * fmt, ...)
{
    if(!debug) return;
    print_prefix ();
    va_list argptr;
    va_start(argptr, fmt);
    vprintf(fmt, argptr);
    va_end(argptr);
    printf ("\n");
}

ssize_t send_string (int fd, const char * value)
{
    return send (fd, value, strlen (value) + 1, 0);
}

ssize_t send_int (int fd, int value)
{
    int size = snprintf (NULL, 0, "%d", value);
    char buffer [size + 1];
    snprintf (buffer, size + 1, "%d", value);
    buffer [size] = 0;
    log("%d (%s) has size %d", value, buffer, size);
    return send_string (fd, buffer);
}

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
        fprintf (stderr, "Shim: Path %s is too long!", path);
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

    chmod (path, 0660);

    return true;
}

pid_t spawn (int fd, char * params [])
{
    assert (params != 0);
    assert (*params != 0);
    log1 ("Spawning!");
    pid_t child = fork ();
    if (child)
        return 0;

    log1 ("Forked!");

    if (setsid () == -1) {
        perror ("setsid");
        exit(1);
    }

    child = fork ();
    if (child) {
        send_int (fd, child);
        log ("Sent pid %d", child);
        exit (0);
        return -1;
    }

    log1 ("Reforked!");

    log ("Running %s", *params);

    if (execv (*params, params) == -1) {
        perror ("execv");
        exit (1);
    }

    return -1;
}

bool run_connection (int fd, char * params [])
{
    assert (fd != 0);
    assert (params != 0);
    assert (*params != 0);
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
            pid_t spawned = spawn (fd, params);
            if (spawned != 0) {
                log1 ("Sending NOK");
                if (send_string (fd, "NOK\n") < 0) {
                    perror ("send");
                    return false;
                }
            }
        } else {
            log1 ("Sending NACK!");
            if (send_string (fd, "NACK!\n") < 0) {
                perror ("send");
                return false;
            }
        }
    }
}

int main (int argc, char * argv [], char *envp[])
{
    uid_t euid = geteuid ();
    setreuid (euid, euid);
    gid_t egid = getegid ();
    setregid (egid, egid);

    char * verbose_env = getenv("VERBOSE");
    char * debug_env = getenv("DEBUG");
    verbose = verbose_env && (verbose_env[0] == 'y' || verbose_env[0] == 'Y');
    debug = debug_env && (debug_env[0] == 'y' || debug_env[0] == 'Y');

    log1 ("Started.");
    log ("I'm uid %d euid %d gid %d egid %d", getuid (), geteuid (), getgid (), getegid ());

    if (argc <= 2) {
        fprintf (stderr, "Usage: %s <socket> <command>\n", argv [0]);
        return 1;
    }

    const char * path = argv [1];
	char ** params = argv + 2;

    log ("Will run %s", *params);

    int local_fd;
    if (!start_server (path, &local_fd))
        return 1;

    for (;;) {
        log1 ("Waiting for a connection...");
        struct sockaddr_un remote;
        socklen_t t = sizeof (remote);
        int remote_fd = accept (local_fd, (struct sockaddr *)&remote, &t);
        if (remote_fd == -1) {
            perror ("accept");
            return 1;
        }

        log1 ("Connected.");

        if (!run_connection (remote_fd, params))
            log1 ("Something went wrong while processing input");

        close (remote_fd);
    }
}
