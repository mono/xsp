<%@ Page Language="C#" %>
<%@ Register TagPrefix="mono" TagName="MonoSamplesHeader" src="~/controls/MonoSamplesHeader.ascx" %>
<html>
<head>
<link rel="stylesheet" type="text/css" href="/mono-xsp.css">
<script runat="server">
	void Clicked (Object sender, EventArgs e) 
	{
	}
</script>
<title>LinkButton as submit and command</title>
</head>
<body><mono:MonoSamplesHeader runat="server"/>
<form runat="server">
<asp:LinkButton id="lb1" Text="Push me!" OnClick="Clicked"
runat="server"/>
<br>
<asp:LinkButton id="lb2" command="Remove_this" runat="server">
Remove this link.
</asp:LinkButton>
</form>
</body>
</html>

