<%@ Page Language="C#" %>
<script runat="server">
	void Page_Load(Object sender, EventArgs e)
	{
		ControlCollection cc = ph.Controls;
		CheckBox chk = new CheckBox ();
		chk.Text = "Wow?";
		cc.Add (chk);

		cc.Add (new LiteralControl ("\n<br>\n"));
		HyperLink lnk = new HyperLink ();
		lnk.NavigateUrl = "http://www.go-mono.com";
		lnk.Text = "Mono project Home Page";
		lnk.Target="_top";
		cc.Add (lnk);
	}
</script>
<html>
<title>PlaceHolder with a CheckBox and a HyperLink added in Page_Load</title>
<body>
<form runat="server">
	<asp:PlaceHolder id="ph" runat="server"/>
</form>
</body>
</html>

