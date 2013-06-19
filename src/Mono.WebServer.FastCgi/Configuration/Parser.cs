namespace Mono.WebServer.FastCgi.Configuration {
	delegate bool Parser<T> (string input, out T output);
}