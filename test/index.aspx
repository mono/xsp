<%@ language="C#" EnableSessionState="false" %>
<%@ Import namespace="System.IO" %>
<html>
<head>
<title>Welcome to Mono XSP!</title>
<link href="favicon.ico" rel="SHORTCUT ICON" />
<link rel="stylesheet" type="text/css" href="mono-xsp.css">
</head>
<body>

<table border="0" cellpadding="0" cellspacing="0" bgcolor="#555555" width="100%">
<tr><td colspan="2">&nbsp;</td></tr>
<tr><td rowspan="2" width="50" align="center" valign="center"><a href="http://www.go-mono.com"><img src="monobutton.png" alt="http://www.go-mono.com" border="0"></a></td>
<td valign="top"><h1 class="title">Welcome to Mono XSP!</h1></td>
</tr>
<tr><td valign="top"><span class="subtitle">XSP is a simple web server written in C# that can be used to run your ASP.NET applications</span></td></tr>
<tr><td colspan="2">&nbsp;</td></tr>
</table>

<p>Here are some ASP.NET examples:</p>
<%
DirectoryInfo dir = new DirectoryInfo (Path.GetDirectoryName (Request.PhysicalPath));
FileInfo[] files = dir.GetFiles ();
StringBuilder sb = new StringBuilder ();
Hashtable styles = new Hashtable ();
styles [".aspx"] = "background: #f1f1ed";
styles [".ashx"] = "background: #00cccc";
styles [".asmx"] = "background: #eeee00";
for (int i=0; i < files.Length; i++) {
	string fileName = Path.GetFileName(files[i].FullName);
	string extension = Path.GetExtension (files[i].FullName);
	if (styles.Contains (extension)) {
		sb.AppendFormat ("<img src=\"small-icon.png\" />&nbsp; <a style=\"{1}\" href=\"{0}\">{0}</a><br/>\n", fileName, styles [extension]);
	}
}
FileList.Text = sb.ToString ();
%>
<ul>
<asp:Label id="FileList" runat="server" />
</ul>
<hr />
<table width="100%">
<tr>
<td width="100%">Generated: <%= DateTime.Now %></td>
<td><img src="mono-powered-big.png"/></td> 
</html>

