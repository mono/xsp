<%@ Page Language="c#" %>
<script runat="server">

    void Menu1_MenuItemClick(Object s, 
     System.Web.UI.WebControls.MenuEventArgs e)
    {
        Label1.Text = "You selected " + e.Item.Text;
		Page.Header.Metadata ["un"] = "dos";
		Page.Header.Title = "You selected " + e.Item.Text;
		
		Style st = new Style ();
		st.Width=10;
		Page.Header.StyleSheet.RegisterStyle (st, null);
		Page.Header.StyleSheet.RegisterStyle (st, null);
		Page.Header.StyleSheet.CreateStyleRule (st, "BODY", null);
    }
    
</script>
<html>
<head runat="server">
    <title>Simple Menu</title>
	<META kk="iii">
</head>
<body>
    <form id="form1" runat="server">

    <asp:Menu
        id="Menu1"
		StaticDisplayLevels = "1"
        OnMenuItemClick="Menu1_MenuItemClick"
		Orientation="Vertical"
		DynamicHorizontalOffset = "5"
		DynamicVerticalOffset = "0"
		DynamicHoverStyle-ForeColor = "Green"
		StaticHoverStyle-BackColor = "Magenta"
		DynamicMenuStyle-BackColor = "LemonChiffon"
		DynamicMenuStyle-ForeColor = "gray"
		DynamicSelectedStyle-BackColor = "Red"
        Runat="Server">
	<DynamicItemTemplate>
	Item: <%# ((MenuItem)Container.DataItem).Text %>
	</DynamicItemTemplate>
    <Items>
    <asp:MenuItem Text="Part I">
        <asp:MenuItem Text="Chapter 1" ImageUrl="http://msdn.microsoft.com/msdn-online/shared/graphics/icons/rtg_email.gif"/>
        <asp:MenuItem Text="Chapter 2 bsdfsdf" />
        <asp:MenuItem Text="Chapter 3 aux">
			<asp:MenuItem Text="Chapter 3.1" />
			<asp:MenuItem Text="Chapter 3.2" />
			<asp:MenuItem Text="Chapter 3.3">
				<asp:MenuItem Text="Chapter 3.3.1" />
				<asp:MenuItem Text="Chapter 3.3.2" />
			</asp:MenuItem>
	    </asp:MenuItem>
        <asp:MenuItem Text="Chapter 4" />
    </asp:MenuItem>
    <asp:MenuItem Text="Part II">
        <asp:MenuItem Text="Chapter 5" />
        <asp:MenuItem Text="Chapter 6" />
    </asp:MenuItem>
    <asp:MenuItem Text="Part III"/>
    </Items>
    </asp:Menu>

    <p>&nbsp;</p>
    <asp:Label ID="Label1" Runat="Server" />
	
    </form>
	<br/>
</body>
</html>
