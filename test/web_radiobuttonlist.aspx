<%@ Page Language="C#" %>
<html>
<head>
<title>RadioButtonList</title>
</head>
<body>
<form runat="server">
Horizontal:
<p>
<asp:RadioButtonList id="rbl1" repeatDIrection="horizontal" runat="server">
	<asp:ListItem>Seven</asp:ListItem>
	<asp:ListItem>Eleven</asp:ListItem>
	<asp:ListItem>Thirteen</asp:ListItem>
	<asp:ListItem>Seventeen</asp:ListItem>
	<asp:ListItem>Twenty-three</asp:ListItem>
	<asp:ListItem>Twenty-nine</asp:ListItem>
</asp:RadioButtonList>
<p>
Vertical:
<p>
<asp:RadioButtonList id="rbl2" repeatDirection="vertical" runat="server">
	<asp:ListItem>Seven</asp:ListItem>
	<asp:ListItem>Eleven</asp:ListItem>
	<asp:ListItem>Thirteen</asp:ListItem>
	<asp:ListItem>Seventeen</asp:ListItem>
	<asp:ListItem>Twenty-three</asp:ListItem>
	<asp:ListItem>Twenty-nine</asp:ListItem>
</asp:RadioButtonList>
</form>
</body>
</html>

