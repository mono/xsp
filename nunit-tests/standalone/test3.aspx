<%@ Page language="c#" AutoEventWireup="true" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN" >
<!-- bug50154: unexpected onclick for button -->
<HTML>
	<HEAD>
		<title>WebForm3</title>
		<script language="C#" runat="server">
		private void Page_Load(object sender, System.EventArgs e)
		{
			if (IsPostBack)
				Label1.Text = "Not suppose to happen";
		}
		</script>
		<meta name="GENERATOR" Content="Microsoft Visual Studio .NET 7.1">
		<meta name="CODE_LANGUAGE" Content="C#">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
	</HEAD>
	<body MS_POSITIONING="GridLayout">
		<form id="Form1" method="post" runat="server">
			<asp:Label id="Label1" runat="server" />
			<INPUT style="Z-INDEX: 101; LEFT: 48px; WIDTH: 72px; POSITION: absolute; TOP: 16px; HEIGHT: 24px"
				type="button" value="HtmlButton">
		</form>
	</body>
</HTML>
