<%@Page %>
<script runat="server" language="c#">
void Page_Load(object sender, EventArgs e)
{
	if (!Page.IsPostBack) 
	{
		MyList.Enabled=false;
		MyText.Text = "Hi Friends";
		MyText.Enabled=false;
	}
}
</script>
<html><body><form runat="server">

<asp:CheckBoxList id="MyList" runat="server">
	<asp:ListItem Value="Hello" />
</asp:CheckBoxList>
<asp:TextBox id="MyText" runat="server" Text="Hi" />
<asp:Button runat="server" id="btn" Text="Click Me" />	
</form></body></html>
