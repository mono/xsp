<%@ language="C#" %>
<html>
<script runat=server>
	void Clicked (object o, EventArgs e)
	{
	}
</script>
<head>
<title>CheckBox</title>
</head>
<body>
<form runat="server">
<asp:CheckBox id="chk"
	Text="Click here!"
	AutoPostBack="True"
	OnCheckedChanged="Clicked"
	runat="server"/>
<br>
<asp:CheckBox id="chk2"
	Text="Click also here!"
	AutoPostBack="True"
	align=right
	OnCheckedChanged="Clicked"
	runat="server"/>
</form>
</body>
</html>
