<html>
<head>
<script language="C#" runat="server">
	void Click (object o, EventArgs e) 
	{
		 lbl.Text = "You selected '" + ddl.SelectedItem.Text +
			    "' (index #" + ddl.SelectedIndex + ").";  
	}
</script>
<title>DropDownList</title>
</head>
<h3>DropDownList test</h3>
<body>
	<form runat="server">
		<asp:DropDownList id="ddl" runat="server">
			<asp:ListItem>Item 1</asp:ListItem>
			<asp:ListItem>Item 2</asp:ListItem>
			<asp:ListItem>Item 3</asp:ListItem>
			<asp:ListItem>Item 4</asp:ListItem> 
		</asp:DropDownList>
		<br><br>
		<asp:Button id="btn" Text="Submit"
			    OnClick="Click" runat="server"/>
		<hr>
		<asp:Label id="lbl" runat="server"/>
	</form>
</body>

