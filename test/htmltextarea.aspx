<%@ language="c#" %>
<html>
<script runat="server">
	void Page_Load (object sender, EventArgs e) 
	{
		 myTA.InnerText = "Hi there!\nCool!";
	}
</script>
<head>
<title>HtmlTextArea</title>
</head>
<body>
<form runat="server">
<textarea id="myTA" cols=25 rows=5 runat="server" />
</form>
</body>
</html>

