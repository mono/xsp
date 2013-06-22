namespace Mono.WebServer.Options {
	public delegate bool Parser<T> (string input, out T output);
}