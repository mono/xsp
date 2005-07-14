<script runat="server" language="c#">
	void Dump (string s, object o, EventArgs args)
	{
		Console.WriteLine (s);
		//Console.WriteLine ("Object is: {0}", o.GetType ().BaseType);
		//Console.WriteLine ("Args is: {0} {1} {2}", args.GetType (), args.ToString (), args == EventArgs.Empty);
		//Console.WriteLine (Environment.StackTrace);
	}

	void Application_Start (object o, EventArgs args)
	{
		Dump ("Start", o, args);
	}

	void Application_End (object o, EventArgs args)
	{
		Dump ("End", o, args);
		Console.WriteLine (Environment.StackTrace);
	}

	void Application_BeginRequest (object o, EventArgs args)
	{
		Dump ("BeginRequest", o, args);
		if (Request.FilePath == "/index.aspx" && Environment.Version.Major == 2)
			Response.Redirect ("/index2.aspx");
	}

	void Application_AuthenticateRequest (object o, EventArgs args)
	{
		Dump ("AuthenticateRequest", o, args);
	}	

	void Application_AuthorizeRequest (object o, EventArgs args)
	{
		throw new Exception ();
		Dump ("AuthorizeRequest", o, args);
	}	
	void Application_ResolveRequestCache (object o, EventArgs args)
	{
		Dump ("ResolveRequestCache", o, args);
	}	
	void Application_AcquireRequestState (object o, EventArgs args)
	{
		Dump ("AcquireRequestState", o, args);
	}	

	void Application_PreRequestHandlerExecute (object o, EventArgs args)
	{
		Dump ("PreRequestHandlerExecute", o, args);
	}	

	//

	void Application_PostRequestHandlerExecute (object o, EventArgs args)
	{
		Dump ("PostRequestHandlerExecute", o, args);
	}	

	void Application_ReleaseRequestState (object o, EventArgs args)
	{
		Dump ("ReleaseRequestState", o, args);
	}	

	void Application_UpdateRequestCache (object o, EventArgs args)
	{
		Dump ("UpdateRequestCache", o, args);
	}	

	void Application_EndRequest (object o, EventArgs args)
	{
		Dump ("EndRequest", o, args);
	}	

	void Application_PreSendRequestHeaders (object o, EventArgs args)
	{
		Dump ("PreSendRequestHeaders", o, args);
	}	
	void Application_PreSendRequestContent (object o, EventArgs args)
	{
		Dump ("PreSendRequestContent", o, args);
	}	
	void Application_Error (object o, EventArgs args)
	{
		Dump ("Error", o, args);
	}	

	
</script>
