<%@ language="C#" %>
<html>
<script runat=server>
	void Clicked (object o, EventArgs e)
	{
	}
</script>
<head>
<title>Button</title>
</head>
<body>
<form runat="server">
<asp:Button id="btn"
     Text="Submit"
     OnClick="Clicked"
     runat="server"/>
</form>
</body>
</html>
