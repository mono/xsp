<%@ language="C#" %>
<html>
<script runat=server>
	void txt_Changed (object sender, EventArgs e)
	{
	}
</script>
<head>
<title>TextBox: MultiLine, SingleLine and Password</title>
</head>
<body>
<form runat="server">
Multiline:
<br>
<asp:TextBox id="txt1" Text="multiline" TextMode="MultiLine" OnTextChanged="txt_Changed" runat="server" rows=5 />
<br>
Single:
<br>
<asp:TextBox id="txt2" Text="singleline" TextMode="singleLine" OnTextChanged="txt_Changed" runat="server" maxlength=40 />
<br>
Password:
<br>
<asp:TextBox id="txt3" Text="badifyouseethis" TextMode="password" OnTextChanged="txt_Changed" runat="server" maxlength=15 />
<br>
</form>
</body>
</html>

