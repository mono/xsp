<%@ language="C#" %>
<html>
<head>
<title>HyperLink</title>
</head>
<script runat="server">
	void Page_Load ()
{
	Response.Output.WriteLine (ResolveUrl ("~") + "<br>"); // should be /
	Response.Output.WriteLine (ResolveUrl ("~/") + "<br>"); // should be /
	Response.Output.WriteLine (ResolveUrl ("~/xx.aspx") + "<br>"); // /xx.aspx
	Response.Output.WriteLine (ResolveUrl ("~xx.aspx") + "<br>"); // /~xx.aspx
}
</script>
<body>
</body>
</html>
