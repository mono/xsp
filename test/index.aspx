<%@ language="C#" EnableSessionState="false" AutoEventWireup="false" %>
<%@ Assembly Name="SiteMapReader_1.1" %>
<%@ Import namespace="System.IO" %>
<%@ Import Namespace="Samples" %>
<%@ Register TagPrefix="mono" TagName="MonoSamplesHeader" src="~/controls/MonoSamplesHeader.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN">
<html>
<head>
<title>Welcome to Mono XSP!</title>
<link href="favicon.ico" rel="SHORTCUT ICON" />
<link rel="stylesheet" type="text/css" href="/mono-xsp.css">
<meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1">
<script runat="server">
	protected override void OnLoad (EventArgs args)
	{
		base.OnLoad (args);
	        SiteMapReader smr = Application ["SiteMapReaderInstance"] as SiteMapReader;
	        if (smr == null) {
 		       DirectoryInfo dir = new DirectoryInfo (Path.GetDirectoryName (Request.PhysicalPath));
		       StringBuilder sb = new StringBuilder ();
		       sb.Append ("<ul class=\"dirlist\">\n");
		       sb.Append (ReadDirectory (Path.Combine (Path.GetDirectoryName (Request.PhysicalPath), "1.1"), "1.1/"));
		       sb.Append ("</ul>");
		       fileList.InnerHtml = sb.ToString ();
	        } else
	               fileList.InnerHtml = smr.RenderNavigationList ();

		if (File.Exists ("Makefile") && File.Exists ("Makefile.am")){
			warning.InnerText = "You are running XSP on the distribution directory, not the installed directory.  Not all samples will work";
		}
	}
	
	public string ReadDirectory (string path, string basePath)
	{
		StringBuilder sb = new StringBuilder ();
		foreach (string sdir in Directory.GetDirectories (path)) {
			string s = ReadDirectory (sdir, basePath + Path.GetFileName (sdir) + "/");
			if (s != "")
				sb.AppendFormat ("<li><b>{0}</b><ul>{1}</ul></li>", Path.GetFileName (sdir), s);
		}
		foreach (string file in Directory.GetFiles (path)) {
			string fileName = basePath + Path.GetFileName (file);
			if (fileName == "index.aspx") continue;
			string extension = Path.GetExtension (file);
			if (extension == ".aspx" || extension == ".ashx" || extension == ".asmx") {
				sb.AppendFormat ("<li><a class=\"{2}\" href=\"{1}\">{0}</a></li>\n",
						Path.GetFileName (fileName), fileName, extension.Substring (1));
			}
		}
		return sb.ToString ();
	}
	
</script>
</head>
<body>
<mono:MonoSamplesHeader runat="server"/>
<div id="warning" runat="server" style="background: red;"></div>

<p>Here are some ASP.NET examples:</p>
<span id="fileList" runat="server" />
<hr />
<div>
<img style="float:right" src="mono-powered-big.png" alt="Mono Powered">
<div style="text-align: left; font-size: small;">Generated: <%= DateTime.Now %></div>
</div>
</body>
</html>

