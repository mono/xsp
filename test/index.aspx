<%@ language="C#" %>
<%@ Import namespace="System.IO" %>
<html>
<head>
<title>Welcome to Mono XSP!</title>
</head>
<body>
<h1>Welcome to Mono XSP!</h1>
<a href="http://www.go-mono.com"><img src="mono.png" alt="http://www.go-mono.com"></a>
<p>Here are some ASP.NET examples:</p>
<%
DirectoryInfo dir = new DirectoryInfo (MapPath ("/"));
FileInfo[] files = dir.GetFiles ();
StringBuilder sb = new StringBuilder ();
Hashtable styles = new Hashtable ();
styles [".aspx"] = "background: #ffffff";
styles [".ashx"] = "background: #00cccc";
styles [".asmx"] = "background: #eeee00";
for (int i=0; i < files.Length; i++) {
	string fileName = Path.GetFileName(files[i].FullName);
	string extension = Path.GetExtension (files[i].FullName);
	if (styles.Contains (extension)) {
		sb.AppendFormat ("<li><a style=\"{1}\" href=\"{0}\">{0}</a></li>\n", fileName, styles [extension]);
	}
}
FileList.Text = sb.ToString ();
%>
<ul>
<asp:Label id="FileList" runat="server" />
</ul>
<hr />
<small>Generated: <%= DateTime.Now %></small>
</html>

