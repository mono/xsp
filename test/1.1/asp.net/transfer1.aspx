<%@ Page Language="C#" Debug="true" %>
<script runat="server">
	void Page_Load ()
	{
		Server.Transfer ("transfer2.aspx");
	}
</script>
<html>
<body>
This will never be seen on the browser. Miguel sucks.
</body>
</html>

