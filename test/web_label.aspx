<%@ language="C#" %>
<html>
<script runat=server>
	void Page_Load (object o, EventArgs e)
	{
		lbl1.Text += ". This added in Page_Load.";
	}
</script>
<head>
<title>Button</title>
</head>
<body>
<form runat="server">
<asp:label id="lbl1" Text="Text as property" runat="server"/>
<br>
<asp:label id="lbl2" runat="server">Text between tags</asp:label>
</form>
</body>
</html>

