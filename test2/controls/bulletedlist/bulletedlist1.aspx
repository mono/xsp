<%@ Page Language="C#" %>
<html>
<head>

</head>
<body>

<form runat="server">
	<h1>BulletStyle</h1>
	<dl>	
		<dt>NotSet</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="NotSet"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		
		<dt>Numbered</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="Numbered"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		
		<dt>LowerAlpha</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="LowerAlpha"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		
		<dt>UpperAlpha</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="UpperAlpha"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		
		<dt>UpperRoman</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="UpperRoman"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		
		<dt>Disc</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="Disc"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		
		<dt>Circle</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="Circle"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		
		<dt>Square</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="Square"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		
		<dt>CustomImage</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="CustomImage"
				BulletImageUrl="MonoIcon.png"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
	</dl>
	
	<h1>FirstBulletNumber</h1>
	<dl>	
		<dt>Not Set-- non-numeric</dt>
		<dd>
			<asp:BulletedList
				runat="server"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		
		<dt>Not Set -- numeric</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="Numbered"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		<dt>Set-- non-numeric</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				FirstBulletNumber="2"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
		
		<dt>Set -- numeric</dt>
		<dd>
			<asp:BulletedList
				runat="server"
				BulletStyle="Numbered"
				FirstBulletNumber="2"
			>
				<asp:ListItem>Hello,</asp:ListItem>
				<asp:ListItem>World</asp:ListItem>
			</asp:BulletedList>
		</dd>
	</dl>
	
</form>

</body>