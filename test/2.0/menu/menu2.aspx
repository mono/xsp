<%@ Page Language="c#" %>
<%@ Register TagPrefix="mono" TagName="MonoSamplesHeader" src="~/controls/MonoSamplesHeader.ascx" %>
<html>
<head runat="server">
    <title>Simple Menu</title>
    <link rel="stylesheet" type="text/css" href="/mono-xsp.css">
	<META kk="iii">
</head>
<body>
    <mono:MonoSamplesHeader runat="server"/>
    <form id="form1" runat="server">

    <asp:Label ID="Label1" Runat="Server" />
	
	
    <asp:Menu ID="Menu12"
        Orientation="Horizontal"
		DynamicHoverStyle-BackColor = "Red"
		StaticHoverStyle-BackColor = "Red"
        Runat="Server">

  <StaticMenuStyle 
    BackColor="#eeeeee"
    BorderColor="Black"
    BorderStyle="Solid"
    BorderWidth="10" />

  <StaticMenuItemStyle
    HorizontalPadding="5" />      

  <DynamicMenuStyle
    BorderColor="Black"
    BorderStyle="Solid"
    BorderWidth="10" />

  <DynamicMenuItemStyle
    BackColor="#eeeeee"
    HorizontalPadding="5" 
    VerticalPadding="3" />  

  <DynamicSelectedStyle
    BackColor="yellow"
     />  
	
    <Items>
    <asp:MenuItem Text="File">
        <asp:MenuItem 
            Text="New" />
        <asp:MenuItem 
            Text="Open..." />
        <asp:MenuItem 
            Text="Close" />
    </asp:MenuItem>
    <asp:MenuItem Text="Edit">
        <asp:MenuItem 
            Text="Cut"  
            ImageUrl="stock_cut_24.png" />
        <asp:MenuItem 
            Text="Copy"  
            ImageUrl="stock_copy_24.png" />
        <asp:MenuItem 
            Text="Paste" 
            ImageUrl="stock_paste_24.png" 
            />
			
			<asp:MenuItem Text="Edit">
				<asp:MenuItem 
					Text="Cut"  
					ImageUrl="stock_cut_24.png" />
				<asp:MenuItem 
					Text="Copy"  
					ImageUrl="stock_copy_24.png" />
				<asp:MenuItem 
					Text="Paste" 
					ImageUrl="stock_paste_24.png" 
					/>
				<asp:MenuItem Text="Select All" />
			</asp:MenuItem>
			
			
        <asp:MenuItem Text="Select All" />
		
			<asp:MenuItem Text="A large one">
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
				<asp:MenuItem Text="Option 1" />
			</asp:MenuItem>
    </asp:MenuItem>
    </Items>
    </asp:Menu>    
    </form>
	<br/>
	<br/>
	<br/>
	<br/>
	<br/>
	<br/>
	<br/>
	<br/>
	<br/>
	<br/>
</body>
</html>
