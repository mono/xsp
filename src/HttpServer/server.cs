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

class MyWorkerRequest
{
	private string fileName;
	private string csFileName;
	private TextReader input;
	private TextWriter output;
	private ArrayList references;
	private string className;
	private string full_request;
	private string method;
	private string query;
	private string query_options;

	private MyWorkerRequest ()
	{
	}

	public MyWorkerRequest (TextReader input, TextWriter output)
	{
		if (input == null || output == null)
			throw new ArgumentNullException ();

		this.input = input;
		this.output = output;
		references = new ArrayList ();
		AddReference ("MyForm.dll");
		AddReference ("System.Web");
		AddReference ("System.Drawing");
	}

	public void ProcessRequest ()
	{
		GetRequest ();
		Console.WriteLine ("Running xsp...");
		Xsp ();
		Console.WriteLine ("Xsp finished...");
		StreamReader st_file = new StreamReader (File.OpenRead (csFileName));
		StringReader file_content = new StringReader (st_file.ReadToEnd ()); //FIXME
		GetBuildOptions (file_content);
		Page page = Build ();
		SetupPage (page);
	}
	
	private void GetRequest ()
	{
		full_request = input.ReadLine ();
		string req = full_request.Trim ();
		if (0 == String.Compare ("GET ", req.Substring (0, 4), true))
			method = "GET";
		else if (0 == String.Compare ("POST ", req.Substring (0, 5), true))
			method = "POST";
		else
			throw new InvalidOperationException ("Unrecognized method in query: " + full_request);

		fileName = full_request.Substring (method.Length);
		fileName = fileName.Trim ();
		if (fileName [0] == '/')
			fileName = fileName.Substring (1);

		int end = fileName.IndexOf (' ');
		fileName = fileName.Substring (0, end);
		Console.WriteLine ("File name: {0}", fileName);
		csFileName = fileName.Replace (".aspx", ".cs");
	}
	
	private void Xsp ()
	{
		StringWriter xsp_out = new StringWriter (new StringBuilder ());
		TextWriter old_out = Console.Out;
		Console.SetOut (xsp_out);
		Process proc = Process.Start ("xsp.bat", fileName + " " + csFileName);
		proc.WaitForExit ();
		proc.Close ();
		Console.SetOut (old_out);
		xsp_out.Close ();
	}

	private HttpBrowserCapabilities GetCapabilities ()
	{
		MyCapabilities capab = new MyCapabilities ();
		capab.Add ("Accept", "*/*");

		capab.Add ("Referer", "http://127.0.0.1/");
		capab.Add ("Accept-Language", "es");
		capab.Add ("Accept-Encoding", "gzip, deflate");
		capab.Add ("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.0; .NET CLR 1.0.3705)");
		capab.Add ("Host", "127.0.0.1");
		return capab;
	}
	private void SetupPage (Page page)
	{
		HttpRequest request = new HttpRequest (fileName, "http://127.0.0.1/" + fileName, "");
		request.Browser = GetCapabilities ();
		HttpResponse response = new HttpResponse (output);
		page.ProcessRequest (new HttpContext (request, response));
	}

	private void AddReference (string reference)
	{
		references.Add ("-r");
		references.Add (reference);
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
	
	private void GetBuildOptions (StringReader genCode)
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
				File.Delete (dll);
				Compile (src, dll);
				AddReference (dll.Replace (".dll", ""));
			}
			else if (line.StartsWith ("//<reference ")){
				dll = GetAttributeValue (line, "dll");
				AddReference (dll);
			}
			else
				throw new ApplicationException ("This is the build option line i get:\n" +
								line);
		}
	}

	private Page Build ()
	{
		string dll = fileName.Replace (".aspx", ".dll"); //FIXME
		File.Delete (dll);
		Compile (fileName.Replace (".aspx", ".cs"), dll);
		Assembly page_dll = Assembly.LoadFrom (dll);
		if (page_dll == null)
			throw new ApplicationException ("Error loading generated dll: " + dll);

		return (Page) page_dll.CreateInstance ("ASP." + className);
	}

	private void Compile (string csName, string dllName)
	{
		string [] args = new string [references.Count + 4];
		int i = 0;
		foreach (string reference in references)
			args [i++] = reference;

		args [i++] = "--target";
		args [i++] = "library";
		args [i] = csName;

		string cmdline = "";
		foreach (string arg in args)
			cmdline += " " + arg;

		Console.WriteLine ("Running... mcs.exe {0}", cmdline);
		TextWriter old_out = Console.Out;
		StringBuilder mcs_output = new StringBuilder ();
		StringWriter new_out = new StringWriter (mcs_output);
		Console.SetOut (new_out);
		Process proc = Process.Start ("mcs.exe", cmdline);
		proc.WaitForExit ();
		new_out.Close ();
		proc.Close ();
		Console.SetOut (old_out);
		Console.WriteLine ("{0}\n<!-- Finished compilation of {0} -->", mcs_output, csName);
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
			Console.WriteLine (e.ToString ());
			output.WriteLine ("<html>\n<title>Error</title>\n<body>\n<pre>\n" + e.ToString () +
					  "\n</pre>\n</body>\n</html>\n");
			output.Close ();
		}

		// output is closed in Page.ProcessRequest
		input.Close ();
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
	
	public static void Main (string [] args)
	{
		int port = 80;
		if (args.Length >= 1)
			port = Int32.Parse (args [0]);

		Server server = new Server (port);
		server.Start ();
	}
}

}

