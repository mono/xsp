<%@ Page language="c#" AutoEventWireup="true" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN" >
<HTML>
	<HEAD>
		<title>Page_ID</title>
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<asp:Label id="Label1" style="Z-INDEX: 101; LEFT: 128px; POSITION: absolute; TOP: 48px" runat="server"
				Width="160px" Height="48px">Label</asp:Label>
		</form>
	</body>
	<script language="C#" runat="server">
		private void Page_Load(object sender, System.EventArgs e)
		{
			Page.ID = "Hello";
			Label1.Text = "The Page.ClientID is:" +Page.ClientID;
		}
	</script>
</HTML>
