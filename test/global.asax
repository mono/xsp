<script runat="server" language="c#">
	void Application_Start (object o, EventArgs args)
	{
		Console.WriteLine ("Application_Start");
	}

	void Application_End (object o, EventArgs args)
	{
		Console.WriteLine ("Application_End");
	}

	void Application_BeginRequest (object o, EventArgs args)
	{
		if (Request.FilePath == "/index.aspx" && Environment.Version.Major == 2)
			Response.Redirect ("/index2.aspx");
	}
</script>
