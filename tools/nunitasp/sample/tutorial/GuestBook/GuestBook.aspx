<%@ Page language="c#" Codebehind="GuestBook.aspx.cs" AutoEventWireup="false" Inherits="GuestBook.GuestBook" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://localhost/NUnitAsp/web/dtd/xhtml1-transitional.dtd">
<HTML>
	<HEAD>
		<title>GuestBook</title>
		<meta content="Microsoft Visual Studio 7.0" name="GENERATOR">
		<meta content="C#" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
	</HEAD>
	<body>
		<form id="GuestBook" method="post" runat="server">
			<p><b>Guest Book:</b></p>
			
			<u>Enter your name:</u>
			<table>
			<tr><td>Name:</td><td><asp:textbox id="name" Runat="server"></asp:textbox></td></tr>
			<tr><td>Comments:</td><td><asp:TextBox ID="comments" Runat="server"></asp:TextBox></td></tr>
			</table>
			<asp:Button ID="save" Runat="server" Text="Save" OnClick="Save_Clicked"></asp:Button>

			<br /><br /><u>Previous Guests:</u>
			<asp:DataGrid id="book" runat="server"></asp:DataGrid>
		</form>
	</body>
</HTML>
