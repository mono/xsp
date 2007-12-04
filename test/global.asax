<%@ Import Namespace="System.IO" %>
<script runat="server" language="c#" >
        static bool v2 = Environment.Version.Major == 2;
	static object appV2 = null;

	void Application_Start (object o, EventArgs args)
	{
		Console.WriteLine ("Application_Start");
	        Type type;

		if (v2) {
	                type = Type.GetType ("Samples.Application, App_Code", false);
	                if (type != null)
	                      appV2 = Activator.CreateInstance (type, new object[] {HttpContext.Current});
	        } else {
	                type = Type.GetType ("Samples.SiteMapReader, SiteMapReader_1.1", false);
	                if (type != null) {
	                       string siteMapPath = Path.Combine (HttpRuntime.AppDomainAppPath, "Web.sitemap");
	                       object smr = Activator.CreateInstance (type, new object[] {siteMapPath});
	                       if (smr != null) {
	                               Application.Lock ();
	                               Application.Add ("SiteMapReaderInstance", smr);
	                               Application.UnLock ();
	                       } else
	                               SetMissingComponents ();
	                } else
	                       SetMissingComponents ();
	        }
	}

	void SetMissingComponents ()
	{
	        Application.Lock ();
	        Application.Add ("MissingComponentsFlag", true);
	        Application.UnLock ();
	}

	void SetShowingMissingComponents ()
	{
	        Application.Lock ();
	        Application.Add ("ShowingMissingComponentsFlag", true);
	        Application.UnLock ();
	}
	void MissingComponents ()
	{
	        Response.Redirect ("/missing_components.aspx");
	}

	void Application_End (object o, EventArgs args)
	{
		Console.WriteLine ("Application_End");
	}

	void Application_BeginRequest (object o, EventArgs args)
	{
	        if (!v2) {
	             object flag = Application ["ShowingMissingComponentsFlag"];
	             if (flag != null && (bool) flag)
	                    return;

                     flag = Application ["MissingComponentsFlag"];
	             if (flag != null && (bool) flag) {
	                     SetShowingMissingComponents ();
	                     MissingComponents ();
	             }
	        }

		if (v2 &&  Request.FilePath == "/index.aspx")
	                 Response.Redirect ("/index2.aspx");
	}
</script>
