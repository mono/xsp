//
// server.cs: Http server for ASP pages.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Licensed under the terms of the GNU GPL
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//

namespace Mono.ASP {

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.UI;

class MyCapabilities : HttpBrowserCapabilities
{
	private Hashtable capabilities;

	public MyCapabilities ()
	{
		capabilities = new Hashtable ();
	}
	
	public void Add (string key, string value)
	{
		capabilities.Add (key, value);
	}

	public override string this [string value]
	{
		get { return capabilities [value] as string; }
	}
}

class CacheData
{
	private AppDomain domain;
	private string dllName;
	private string className;
	private string fileName;
	private DateTime writeTime;
	private bool unloaded;

	public CacheData (string fileName, string dllName, string className, DateTime writeTime)
	{
		DateTime dt = DateTime.Now;
		this.dllName = dllName;
		domain = AppDomain.CreateDomain (dllName);
		this.fileName = fileName;
		this.dllName = dllName;
		this.className = className;
		this.writeTime = writeTime;
	}

	public bool OlderThan (DateTime date)
	{
		if (unloaded)
			throw new ApplicationException ("This domain is unloaded: " + dllName);

		return (writeTime < date);
	}

	public object CreateInstance ()
	{
		if (unloaded)
			throw new ApplicationException ("This domain is unloaded: " + dllName);

		return domain.CreateInstanceFromAndUnwrap (dllName, className);
	}

	public void Unload ()
	{
		if (unloaded)
			throw new ApplicationException ("This domain is unloaded: " + dllName);

		unloaded = true;
		AppDomain.Unload (domain);
		domain = null;
	}
}

class PageFactory
{
	public class PageBuilder
	{
		private StringBuilder cscOptions;
		private string fileName;
		private string csFileName;
		private string className;
		public static char dirSeparator = Path.DirectorySeparatorChar;
		private static Hashtable cachedData = new Hashtable ();
		private static Random rnd_file = new Random ();

		public static bool CheckDate (string fileName)
		{
			CacheData cached = cachedData [fileName] as CacheData;
			DateTime fileWriteTime = File.GetLastWriteTime (fileName);

			if (cached != null && cached.OlderThan (fileWriteTime)) {
				cachedData.Remove (fileName);
				cached.Unload ();
				cached = null;
				return false;
			}

			return true;
		}

		private PageBuilder ()
		{
		}

		public PageBuilder (string fileName)
		{
			this.fileName = fileName;
			csFileName = "xsp_" + Path.GetFileName (fileName).Replace (".aspx", ".cs");

			cscOptions = new StringBuilder ();
#if MONO
			cscOptions.Append ("--target library ");
			cscOptions.Append ("-L . ");
			AddReference ("corlib");
			AddReference ("System");
			AddReference ("System.Data");
#else
			cscOptions.Append ("/noconfig ");
			cscOptions.Append ("/nologo ");
			cscOptions.Append ("/debug+ ");
			cscOptions.Append ("/debug:full ");
			cscOptions.Append ("/target:library ");
			AddReference ("mscorlib.dll ");
			AddReference ("System.dll ");
			AddReference ("System.Data.dll ");
			AddReference (".\\MyForm.dll ");
#endif
			AddReference (Server.SystemWeb);
			AddReference (Server.SystemDrawing);
		}

		public Page Build ()
		{
			CacheData cached = cachedData [fileName] as CacheData;
			string dll;
			DateTime fileWriteTime = File.GetLastWriteTime (fileName);

			if (cached != null && cached.OlderThan (fileWriteTime)) {
				cachedData.Remove (fileName);
				cached.Unload ();
				cached = null;
			}
			
			if (cached == null) {
				if (Xsp (fileName, csFileName) == false){
					Console.WriteLine ("Error running xsp. " + 
							   "Take a look at the output file.");
					return null;
				}

				StreamReader st_file = new StreamReader (File.OpenRead ("output" +
											dirSeparator +
											csFileName));
				
				StringReader file_content = new StringReader (st_file.ReadToEnd ());
				st_file.Close ();
				if (GetBuildOptions (file_content) == false)
					return null;

				dll = "output" + dirSeparator;
				dll += rnd_file.Next () + Path.GetFileName (fileName).Replace (".aspx", ".dll");
				if (Compile (csFileName, dll) == true){
					cached = new CacheData (fileName,
								dll,
								"ASP." + className,
								fileWriteTime);
					cachedData.Add (fileName, cached);
				}
			}

			if (cached == null)
				return null;

			return GetInstance (cached);
		}

		private static bool Xsp (string fileName, string csFileName)
		{
#if MONO
			return RunProcess ("mono", 
					   "xsp.exe " + fileName, 
					   GeneratedXspFileName (fileName),
					   "output" + dirSeparator + "xsp_" + Path.GetFileName (fileName) + 
					   ".sh");
#else
			return RunProcess ("xsp", 
					   fileName, 
					   GeneratedXspFileName (fileName),
					   "output" + dirSeparator + "xsp_" + fileName + ".bat");
#endif
		}

		private static bool RunProcess (string exe, string arguments, string output_file, string script_file)
		{
			Console.WriteLine ("{0} {1}", exe, arguments);
			Console.WriteLine ("Output goes to {0}", output_file);
			Console.WriteLine ("Script file is {0}", script_file);
			Process proc = new Process ();
#if MONO
			proc.StartInfo.FileName = "redirector.sh";
			proc.StartInfo.Arguments = exe + " " + output_file + " " + arguments;
			proc.Start ();
			proc.WaitForExit ();
			int result = proc.ExitCode;
			proc.Close ();

			StreamWriter bat_output = new StreamWriter (File.Create (script_file));
			bat_output.Write ("redirector.sh" + " " + exe + " " + output_file + " " + arguments);
			bat_output.Close ();
#else
			proc.StartInfo.FileName = exe;
			proc.StartInfo.Arguments = arguments;
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.Start ();
			string poutput = proc.StandardOutput.ReadToEnd();
			proc.WaitForExit ();
			int result = proc.ExitCode;
			proc.Close ();

			StreamWriter cmd_output = new StreamWriter (File.Create (output_file));
			cmd_output.Write (poutput);
			cmd_output.Close ();
			StreamWriter bat_output = new StreamWriter (File.Create (script_file));
			bat_output.Write (exe + " " + arguments);
			bat_output.Close ();
#endif

			return (result == 0);
		}

		private bool GetBuildOptions (StringReader genCode)
		{
			string line;
			string dll;

			while ((line = genCode.ReadLine ()) != String.Empty){
				if (line.StartsWith ("//<class ")){
					className = GetAttributeValue (line, "name");
				}
				else if (line.StartsWith ("//<compileandreference ")){
					string src = GetAttributeValue (line, "src");
					dll = src.Replace (".cs", ".dll"); //FIXME
					//File.Delete (dll);
					if (Compile (src, dll) == false){
						Console.WriteLine ("Error compiling {0}. See the output file.", src);
						return false;
					}
					AddReference (dll.Replace (".dll", ""));
				}
				else if (line.StartsWith ("//<reference ")){
					dll = GetAttributeValue (line, "dll");
					AddReference (dll);
				}
				else {
					Console.WriteLine ("This is the build option line i get:\n" + line);
					return false;
				}
			}

			return true;
		}

		private void AddReference (string reference)
		{
			string arg;
#if MONO
			arg = String.Format ("-r {0} ", reference);
#else
			arg = String.Format ("/r:{0} ", reference);
#endif
			cscOptions.Append (arg);
		}
		
		private string GetAttributeValue (string line, string att)
		{
			string att_start = att + "=\"";
			int begin = line.IndexOf (att_start);
			int end = line.Substring (begin + att_start.Length).IndexOf ('"');
			if (begin == -1 || end == -1)
				throw new ApplicationException ("Error in reference option:\n" + line);

			return line.Substring (begin + att_start.Length, end);
		}
		
		private static Page GetInstance (CacheData cached)
		{
			return cached.CreateInstance () as Page;
		}

		private bool Compile (string csName, string dllName)
		{
#if MONO
			cscOptions.AppendFormat ("-o {0} ", dllName);
#else
			cscOptions.AppendFormat ("/out:{0} ", dllName);
#endif
			cscOptions.Append ("output" + dirSeparator + csName);

			string cmdline = cscOptions.ToString ();
			string noext = csName.Replace (".cs", "");
			string output_file = "output" + dirSeparator + "output_from_compilation_" + noext + ".txt";
			string bat_file = "output" + dirSeparator + "last_compilation_" + noext + ".bat";
#if MONO
			return RunProcess ("mcs", cmdline, output_file, bat_file);
#else
			return RunProcess ("csc.exe", cmdline, output_file, bat_file);
#endif
		}
	}

	private static Hashtable loadedPages = new Hashtable ();


	public static string CompilationOutputFileName (string fileName)
	{
		string name = "xsp_" + Path.GetFileName (fileName).Replace (".aspx", ".txt");
		return "output" + PageBuilder.dirSeparator + "output_from_compilation_" + name;
	}

	public static string GeneratedXspFileName (string fileName)
	{
		string name = Path.GetFileName (fileName).Replace (".aspx", ".cs");
		return "output" + PageBuilder.dirSeparator + "xsp_" + name;
	}

	private PageFactory ()
	{
	}

	public static Page GetPage (string fileName, string query_options)
	{
#if MONO
		HttpRequest request = new HttpRequest (fileName, "http://127.0.0.1/" + fileName, 
						       query_options);

		string view_state = request.QueryString ["__VIEWSTATE"];
		if (view_state != null && loadedPages.ContainsValue (view_state)){
			Page p = null;
			foreach (Page _p in loadedPages.Keys){
				if (view_state == loadedPages [_p] as string){
					if (PageBuilder.CheckDate (fileName)) {
						p = _p;
					} else {
						loadedPages.Remove (_p);
					}
					break;
				}
			}

			if (p != null)
				return p;
		}
#endif

		PageBuilder builder = new PageBuilder (fileName);
		Page page = builder.Build ();
#if MONO
		if (page != null)
			loadedPages.Add (page, null);
#endif

		return page;
	}

	public static void UpdateHash (Page page, string new_state)
	{
		if (!(loadedPages.ContainsKey (page)))
			return;

		loadedPages [page] = new_state;
	}

}

class HttpHelpers
{
	public static void SendStatus (TextWriter writer, int code, string message)
	{
		writer.Write ("HTTP/1.0 " + code + " " + message + "\r\n");
	}

	public static void SendHeader (TextWriter writer, string title, string data)
	{
		writer.Write (title + ": " + data + "\r\n");
	}
	
	public static void RenderErrorPage (TextWriter writer, int code, string description, string message)
	{
		/*
		HttpResponse response = new HttpResponse (output);
		response.StatusCode = code;
		response.StatusDescription = message;
		response.ContentType = "text/html";
		string content = String.Format ("<html>\n<title>Error {0}: {1}</title>\n<body>" + 
						"<h2>Error {0}: {1}</h2><p>{2}\n</body>\n</html>\n",
						code, message, description);
		response.Write (content);
		response.End ();
		*/

		SendStatus (writer, code, message);
		SendHeader (writer, "Content-Type", "text/html");
		string content = String.Format ("<html>\n<title>Error {0}: {1}</title>\n<body>" + 
						"<h2>Error {0}: {1}</h2><p>{2}\n</body>\n</html>\n",
						code, description, message);
		SendHeader (writer, "Content-Length", content.Length.ToString ());
		writer.Write ("\r\n");
		writer.Write (content);
	}

	private static void WriteFormattedSource (TextWriter writer, string source)
	{
		StringReader reader = new StringReader (source);
		string line;
		int lineno = 1;
		while ((line = reader.ReadLine ()) != null){
			writer.WriteLine ("line " + lineno + ": " + HttpUtility.HtmlEncode (line));
			lineno++;
		}
		reader.Close ();
	}

	public static void SendDebugPage (TextWriter writer, string fileName)
	{
		string xspOutput = PageFactory.GeneratedXspFileName (fileName);
		if (!File.Exists (xspOutput)) {
			RenderErrorPage (writer, 500, "Internal Server Error", "Couldn't run xsp.exe");
			return;
		}
		
		string compilationOutput = PageFactory.CompilationOutputFileName (fileName);
		Console.WriteLine (compilationOutput);
		if (!File.Exists (compilationOutput)) {
			RenderErrorPage (writer, 500, "Internal Server Error", "xsp.exe failed to generate code");
			return;
		}

		StreamReader csReader = new StreamReader (File.Open (xspOutput, FileMode.Open));
		string csContent = csReader.ReadToEnd ();
		csReader.Close ();

		StreamReader compilationReader = new StreamReader (File.Open (compilationOutput, FileMode.Open));
		string compilationContent = compilationReader.ReadToEnd ();
		compilationReader.Close ();

		StringWriter output = new StringWriter ();
		output.WriteLine ("<html>\n<title>Compilation failed</title>\n<body>");
		output.WriteLine ("<h2>Compilation failed</h2>");
		output.WriteLine ("The output from the compiler is:<p>");
		output.WriteLine ("<table summary=\"\" bgcolor=\"#FFFFCC\">\n<tr>\n<td>");
		output.WriteLine ("<code><pre>");
		output.WriteLine (HttpUtility.HtmlEncode (compilationContent));
		output.WriteLine ("</pre></code>\n</td></tr></table><p>");
		output.WriteLine ("<b>Compilation source code:</b><p>\n");
		output.WriteLine ("<table summary=\"\" bgcolor=\"#FFFFCC\"><tr><td><code><pre>");
		WriteFormattedSource (output, csContent);
		output.WriteLine ("</pre></code></td></tr></table>");
		output.WriteLine ("</body>\n</html>");

		SendStatus (writer, 200, "OK");
		SendHeader (writer, "Content-Type", "text/html");
		string content = output.ToString ();
		output.Close ();
		SendHeader (writer, "Content-Length", content.Length.ToString ());
		writer.Write ("\r\n");
		writer.Write (content);
	}
}

class MyWorkerRequest
{
	private string fileName;
	private string fileOnDisk;
	private TextReader input;
	private TextWriter output;
	private TextWriter outputBuffer;
	private HttpResponse response;

	private string method;
	private string query;
	private string protocol;
	private string query_options = "";
	private int post_size;
	private MyCapabilities headers;
	private static Hashtable mimeHash;
	private static char dirSeparator = Path.DirectorySeparatorChar;

	static MyWorkerRequest ()
	{
		mimeHash = new Hashtable (new CaseInsensitiveHashCodeProvider (),
					  new CaseInsensitiveComparer ());
		mimeHash.Add ("jpg", "image/jpeg");
		mimeHash.Add ("png", "image/png");
		mimeHash.Add ("css", "text/css");
		mimeHash.Add ("aspx", "text/html");
		mimeHash.Add ("htm", "text/html");
		mimeHash.Add ("html", "text/html");
		mimeHash.Add ("txt", "text/plain");
		mimeHash.Add ("xml", "text/xml");
	}
	
	private MyWorkerRequest ()
	{
	}

	public MyWorkerRequest (TextReader input, TextWriter output)
	{
		if (input == null || output == null)
			throw new ArgumentNullException ();

		this.input = input;
		this.output = output;
		outputBuffer = new StringWriter ();
	}

	public void RenderErrorPage (int code, string message, string description)
	{
		HttpHelpers.RenderErrorPage (output, code, message, description);
	}
	
	public void ProcessRequest ()
	{
		if (GetRequestData () == false)
			return;

		if (fileName.IndexOf ("..") != -1){
			RenderErrorPage (400, "Bad request", ".. not allowed in request");
			return;
		}

		fileOnDisk = fileName;
		if (dirSeparator != '/')
			fileOnDisk.Replace ('/', dirSeparator);

		if (!File.Exists (fileOnDisk)){
			RenderErrorPage (404, "Not Found", "File '" + fileName + "' not found.");
			return;
		}

		if (!fileName.EndsWith (".aspx")){
			ProcessRequestNonASPX ();
			return;
		}

		Page page = PageFactory.GetPage (fileOnDisk, query_options);
		if (page == null){
			HttpHelpers.SendDebugPage (output, fileOnDisk);
			return;
		}
		RenderPage (page);
#if MONO
		string new_view_state = page.GetViewStateString ();
		PageFactory.UpdateHash (page, new_view_state);
#endif
	}
	
	void ProcessRequestNonASPX ()
	{
		Stream fileInput = File.Open (fileOnDisk, FileMode.Open); //FIXME
		byte [] fileContent = new byte [8192];
		int offset = 0;
		int count = fileContent.Length;
		Decoder decoder = Encoding.UTF8.GetDecoder ();
		while ((count = fileInput.Read (fileContent, offset, count)) != 0) {
			char [] chars = new char [decoder.GetCharCount (fileContent, 0, count)];
			decoder.GetChars (fileContent, 0, count, chars, 0);
			offset += count;
			outputBuffer.Write (chars);
			chars = null;
		}

		fileInput.Close ();
		SendData ();
	}
	
	private void GetRequestMethod ()
	{
		string req = input.ReadLine ();
		if (req == null)
			throw new ApplicationException ("Void request.");

		if (0 == String.Compare ("GET ", req.Substring (0, 4), true))
			method = "GET";
		else if (0 == String.Compare ("POST ", req.Substring (0, 5), true))
			method = "POST";
		else
			throw new InvalidOperationException ("Unrecognized method in query: " + req);

		req = req.TrimEnd ();
		int idx = req.IndexOf (' ') + 1;
		if (idx >= req.Length)
			throw new ApplicationException ("What do you want?");

		string page_protocol = req.Substring (idx);
		int idx2 = page_protocol.IndexOf (' ');
		if (idx2 == -1)
			idx2 = page_protocol.Length;
		
		query = page_protocol.Substring (0, idx2);
		protocol = page_protocol.Substring (idx2);
	}

	private void GetCapabilities ()
	{
		headers = new MyCapabilities ();
		if (protocol == "")
			return;
		
		string line;
		int idx;
		while ((line = input.ReadLine ()) != "") {
			if (line == null){
				headers.Add ("Accept", "*/*");

				headers.Add ("Referer", "http://127.0.0.1/");
				headers.Add ("Accept-Language", "es");
				headers.Add ("Accept-Encoding", "gzip, deflate");
				headers.Add ("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; " + 
							   "Windows NT 5.0; .NET CLR 1.0.3705)");
				headers.Add ("Host", "127.0.0.1");
				return;
			}

			idx = line.IndexOf (':');
			if (idx == -1 || idx == line.Length - 1){
				Console.Error.WriteLine ("idx: {0} Ignoring request header: {1}", idx, line);
				continue;
			}
			string key = line.Substring (0, idx);
			string value = line.Substring (idx + 1);
			headers.Add (key, value);
			if (key == "Content-Length")
				post_size = Int32.Parse (value.Trim ());
		}
	}
	
	private void GetQueryOptions ()
	{
		if (method == "POST") {
			char [] line = new char [post_size];
			input.Read (line, 0, post_size);
			query_options = new string (line);
		}
		
		if (query_options == null)
			query_options = "";
	}
	
	private bool GetRequestData ()
	{
		GetRequestMethod ();
		GetCapabilities ();
		GetQueryOptions ();

		if (query [0] == '/')
			query = query.Substring (1);

		int end = query.IndexOf (' ');
		string target;
		if (end != -1)
			target = query.Substring (0, end);
		else
			target = query;

		int qmark = target.IndexOf ('?');
		if (qmark == -1){
			fileName = target;
			return true;
		}
		
		if (qmark == 0){
			RenderErrorPage (400, "Bad Request", "Malformed query string.");
			return false;
		}

		fileName = target.Substring (0, qmark);
		if (query_options == "")
			query_options = target.Substring (qmark + 1);
		return true;
	}
	
	private void RenderPage (Page page)
	{
		HttpRequest request = new HttpRequest (fileName, "http://127.0.0.1/" + fileName, 
						       query_options);

		request.Browser = headers;
		request.RequestType = method;

		response = new HttpResponse (outputBuffer);
		try {
			page.ProcessRequest (new HttpContext (request, response));
			SendData ();
		} catch (Exception e) {
			Console.WriteLine ("Caught exception rendering page:\n" + e.ToString ());
			HttpHelpers.RenderErrorPage (output, 500, "Internal Server Error",
						     "The server failed to render '" + fileName + "'");
		}
	}

	private void SendData ()
	{
		if (response.StatusCode != 302){
			HttpHelpers.SendStatus (output, 200, "OK");
		} else {
			HttpHelpers.SendStatus (output, 302, "Found");
#if MONO
			HttpHelpers.SendHeader (output, "Location", response.RedirectLocation);
#endif
		}
		HttpHelpers.SendHeader (output, "Host", "127.0.0.1"); // FIXME
		HttpHelpers.SendHeader (output, "Content-Type", GetContentType ());
		string content = outputBuffer.ToString ();
		HttpHelpers.SendHeader (output, "Content-Length", content.Length.ToString ());
		output.Write ("\r\n");
		output.Write (content);
	}

	private string GetContentType ()
	{
		int lastDot = fileName.LastIndexOf ('.');
		if (lastDot == -1 || lastDot + 1 == fileName.Length)
			return "application/octet-stream";

		string suffix = fileName.Substring (lastDot + 1).ToUpper ();
		string contentType = mimeHash [suffix] as string;
		if (contentType == null)
			contentType = "application/octet-stream";
		return contentType;
	}

}

class Worker
{
	private TcpClient socket;
	
	public Worker (TcpClient socket)
	{
		this.socket = socket;
	}

	public void Run ()
	{
		Console.WriteLine ("Started processing...");
		HtmlTextWriter output = new HtmlTextWriter (new StreamWriter (socket.GetStream ()));
		StreamReader input = new StreamReader (socket.GetStream ());
		try {
			MyWorkerRequest proc = new MyWorkerRequest (input, output);
			proc.ProcessRequest ();
		} catch (Exception e) {
			Console.WriteLine ("Caught exception in Worker.Run");
			Console.WriteLine (e.ToString () + "\n" + e.StackTrace);
			output.WriteLine ("<html>\n<title>Error</title>\n<body>\n<pre>\n" + e.ToString () +
					  "\n</pre>\n</body>\n</html>\n");
		}

		// Under MS may be it throws an exception...?
		try {
			output.Flush ();
		} catch (Exception){
		}

		try {
			output.Close ();
		} catch (Exception){
		}

		try {
			input.Close ();
		} catch (Exception){
		}
		//

		socket.Close ();
		Console.WriteLine ("Finished processing...");
	}
}

public class Server
{
	private TcpListener listen_socket;
	private bool started;
	private bool stop;
	private Thread runner;
	private IPEndPoint bind_address;
	private ArrayList workers;

	public Server ()
		: this (IPAddress.Any, 80)
	{
	}

	public Server (int port)
		: this (IPAddress.Any, port)
	{
	}

	public Server (IPAddress address, int port) 
		: this (new IPEndPoint (address, port))
	{
	}
	
	public Server (IPEndPoint bindAddress)
	{
		if (bindAddress == null)
			throw new ArgumentNullException ("bindAddress");

		bind_address = bindAddress;
	}

	public void Start ()
	{
		if (started)
			throw new InvalidOperationException ("The server is already started.");

		workers = new ArrayList ();
		listen_socket = new TcpListener (bind_address);
		listen_socket.Start ();
		runner = new Thread (new ThreadStart (RunServer));
		runner.Start ();
		stop = false;
		Console.WriteLine ("Server started.");
	}

	public void Stop ()
	{
		if (!started)
			throw new InvalidOperationException ("The server is not started.");

		stop = true;	
		listen_socket.Stop ();
		foreach (Thread th in workers)
			if (th.ThreadState != System.Threading.ThreadState.Stopped)
				th.Abort ();
		workers = null;
		Console.WriteLine ("Server stopped.");
	}

	private void RunServer ()
	{
		started = true;
		try {
			TcpClient client;
			int nrequest = 0;
			while (!stop){
				client = listen_socket.AcceptTcpClient ();
				nrequest++;
				if (nrequest % 1000 == 0)
					CleanupWorkers ();

				Console.WriteLine ("Accepted connection.");
				Worker one_shot = new Worker (client);
				Thread worker = new Thread (new ThreadStart (one_shot.Run));
				workers.Add (worker);
				worker.Start ();
			}
		} catch (ThreadAbortException){
		}

		started = false;
	}
	
	private void CleanupWorkers ()
	{
		ArrayList new_workers = new ArrayList ();

		foreach (Thread th in workers)
			if (th.ThreadState != System.Threading.ThreadState.Stopped)
				new_workers.Add (th);

		workers = new_workers;
	}
	
	private static bool useMonoClasses;

	public static bool UseMonoClasses
	{
		get { return useMonoClasses; }
	}

	public static string SystemWeb
	{
#if MONO
		get { return "System.Web"; }
#else
		get { return (!useMonoClasses ? "System.Web.dll" : ".\\System.Web.dll"); }
#endif
	}

	public static string SystemDrawing
	{
#if MONO
		get { return "System.Drawing"; }
#else
		get { return "System.Drawing.dll"; }
#endif
	}

	private static void Usage ()
	{
		Console.WriteLine ("Usage: server [--usemonoclasses] port");
		Console.WriteLine ("By default, it uses csc to compile against mono " +
				   "System.Web and System.Color, which must be copied\n" +
				   "to the directory where you run the server.");
		Environment.Exit (1);
	}

	private static void RunOneFile (string file_name)
	{
		StringReader fake_input = new StringReader ("get " + file_name + " http/1.0");
		HtmlTextWriter output = new HtmlTextWriter (Console.Out);
		try {
			MyWorkerRequest proc = new MyWorkerRequest (fake_input, output);
			proc.ProcessRequest ();
			output.Flush ();
		} catch (Exception e) {
			Console.WriteLine ("Caught exception processing request.");
			Console.WriteLine (e.ToString ());
		}
	}
	
	public static int Main (string [] args)
	{
		if (args.Length == 0 || args.Length > 3)
			Usage ();

		int port = 80;
		bool useMonoClasses_set = false;
		bool port_set = false;
		bool console_set = false;
		string file_name = "";
		foreach (string arg in args){
			if (console_set){
				file_name = arg;
				break;
			}
			else if (!console_set && 0 == String.Compare (arg, "--file")){
				console_set = true;
			}
			else if (!useMonoClasses_set && 0 == String.Compare (arg, "--usemonoclasses")){
				useMonoClasses = true;
				useMonoClasses_set = true;
			}
			else if (!port_set){
				try {
					port = Int32.Parse (arg);
					port_set = true;
				} catch (Exception){
					Usage ();
				}
			}
			else
				Usage ();
		}

		if (!Directory.Exists ("output")){
			Console.WriteLine ("Creating directory 'output' where BAT files and \n" + 
					   "comand output will be stored.");
			Directory.CreateDirectory ("output");
		}

		if (console_set){
			if (file_name == "")
				Usage ();
			RunOneFile (file_name);
		}
		else {
			
			Console.WriteLine ("Remember that you should rerun the server if you change\n" + 
					   "the aspx file!");

			Server server = new Server (port);
			server.Start ();
		}
		return 0;
	}
}

}

