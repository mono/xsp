<html>
<head>
<script runat="server">
	public void Page_Load (object sender, EventArgs e)
	{
		mySpan.InnerText = "This is ok";
	}
</script>
<title>Just a HtmlGenericControl (a span in this case) fullfilled
in Page_Load ()</title>
</head>
<body>
<span id="mySpan" runat="server" />
</body>
</html>

