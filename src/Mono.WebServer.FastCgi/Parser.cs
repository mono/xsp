namespace Mono.WebServer.FastCgi {
	delegate bool Parser<T> (string input, out T output);
}