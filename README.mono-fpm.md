# Mono-fpm

**mono-fpm is still alpha software**

Mono-fpm is a fastcgi process manager for mono. Its main purpose is to free the system administrator from having to manually configure and start fastcgi daemons on a server that hosts multiple websites for multiple users.

## Architecture

Mono-fpm is composed of three main components:

  - The mono-fpm daemon
  - A C shim
  - The fastcgi-mono-server daemon

## The mono-fpm daemon

This is the master daemon that manages the children instances. It should be run as root as it needs to suid to the different users that own the websites. It reads the configuration files and starts the various children accordingly.

There are two main instance types: static and ondemand.

### Static instances

*TODO*

### Ondemand instances

For each ondemand instance mono-fpm starts a small C shim that listens on an unix socket, whereas mono-fpm itself listen on the socket that will be set in the httpd configuration.
When mono-fpm receives a request it communicates with the C shim that spawns a fastcgi-mono-server instance that subsequently handles connections.

## Configuration

There are two main configuration options for mono-fpm: configuration directories and automatic configuration based on the filesystem layout.

###Configuration files

A small note on configuration files: every executable that is part of xsp accepts `--config-file` (or `--configfile`) as a command line parameter and parses the given file for options. The precedence for options is: default, app settings, environment, xml config, command line.

### Configuration directory

The most flexible way to configure mono-fpm is to use the `--config-dir` parameter. Mono-fpm will then read every xml file in that directory and use it for configuring a single C shim. The suggestion is to use a template file, not giving it an xml extension, and then symlink files with an appropriate name.

An example template follows:

    <Settings>
        <Setting Name="shimsock" Value="/tmp/shims/$(filename)" />
        <Setting Name="ondemandsock" Value="unix://660@/tmp/backs/$(filename)" />
        <Setting Name="instance-type" Value="ondemand" />
        <Setting Name="user" Value="$(filename)" />
        <Setting Name="name" Value="$(filename)" />
        <Setting Name="socket" Value="unix://660@/tmp/sockets/$(filename)" />
        <Setting Name="applications" Value="/:/tmp/website/$(filename)/" />
        <Setting Name="idle-time" Value="20" />
    </Settings>

This will make mono-fpm listen on `/tmp/sockets/$(filename)` and spawn a C shim which will listen on `/tmp/shims/$(filename)`.

While parsing `$(filename)` gets replaced by the actual file name (minus the extension), `$(user)` by the file owner and `$(group)` by the file group.

The main configuration parameters are:

 * **shimsock**: the socket (which *must* be an Unix socket) for the C shim,
 * **ondemandsock**: the socket (which *must* be an Unix socket) for the mono-fpm <-> fastcgi-mono-server communication,
 * **instance-type**: must be set to `ondemand`,
 * **user**: the user that the fastcgi-mono-server will run as, defaults to the file owner,
 * **name**: a name that will appear in verbose logs,
 * **socket**: the socket for the httpd <-> mono-fpm communication, can be either tcp or Unix,
 * **applications**: the applications to run, this will be parsed by fastcgi-mono-server,
 * **idle-time**: the time that the fastcgi-mono-server will stay awake before exiting.

You would then need to setup the httpd to contact mono-fpm on the socket specified with the **socket** parameter. Every configuration file will be passed to the relative fastcgi-mono-server instance so you can use it to pass additional parameters to the fastcgi daemon itself.

An (incomplete) example on how to configure nginx follows:

**Beware, this is just an example, don't use it in production! You've been warned.**

    map $host $user {
		~^(?P<num>[a-z0-9]*)\.example\.com$ $num;
	}

	server {
		listen 80;
		server_name *.example.com;

		access_log /var/log/nginx/localhost.access_log main;
		error_log /var/log/nginx/localhost.error_log info;

		root /tmp/website/user$user;

		 autoindex  on;

		location ~ \.aspx$ {
			try_files $uri =404;
			include /etc/nginx/fastcgi_params;
			fastcgi_pass unix:/tmp/sockets/user$user;
			fastcgi_index index.aspx;
		}
	}

As you can see the `map` directive allows to achieve a single-file configuration.

### Automatic configuration

**Beware, this is the youngest part of mono-fpm, and probably the least secure.**

Just pass `--web-dir` to mono-fpm to specify a directory in which the users' websites are located. Mono-fpm will start one C shim per directory (skipping those with owner uid < 100), and listen on `/tmp/mono-fpm-automatic/front/<directory name>`.

## Permissions

Recent versions of mono-fpm and fastcgi-mono-server allow for specification of permissions on sockets, using the following syntax:

    unix://<perm>@<path>

where perm are the permissions, written in octal.

The permission for the various sockets should be as follows (with "httpd" being the httpd *group*):

### httpd <-> fpm sockets (`socket`)

They should be writable by both fpm and the httpd, and will be created by fpm *while still root*.

Advice: use 660 (rw-rw----) as permission and put it into a directory with permission 2730 (rwx-ws---) and owned by root:httpd.

### fpm <-> shim sockets (`shimsock`)

They should be writable by both fpm and the user, and will be created by the C shim.

Advice: use 660 (rw-rw----) as permission and put it into a directory with permission 3733 (rwx-ws-wt) and owned by root:fpm.

### fpm <-> fastcgi-mono-server sockets (`ondemandsock`)

They should be writable by both fpm and the user, and will be created by the fastcgi-mono-server daemon.

Advice: use 660 (rw-rw----) as permission and put it into a directory with permission 3733 (rwx-ws-wt) and owned by root:fpm.

### Websites directories

They should be writable by the user and readable by nginx.

Advice: use 2750 (rwxr-s---) as permission, chown it to user:httpd and put it into a directory with permission 2711 (rwx--s--x) and owned by root:httpd.

### Configuration directory

This only need to be readable by root.

Advice: use 700 (rwx------) as permission. The files themselves can be chowned to the user if needed for configuration.

## Debug

Passing `--loglevels all --verbose` to mono-fpm will enable a really verbose debug output in which every line is prefixed by `<pid> <thread> <name> <date> <loglevel>`. You can use `sort` on the output to see what happens per-process and per-thread.