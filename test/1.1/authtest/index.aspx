<%@ Page Language="C#" %>
<%@ Import Namespace="System.Web.Security" %>
<html>
<script language="C#" runat=server>
	void Page_Load (object sender, EventArgs e)
	{
		Welcome.Text = "Hello, " + User.Identity.Name;
	}

	void Signout_Click (object sender, EventArgs e)
	{
		FormsAuthentication.SignOut ();
		Response.Redirect ("/1.1/authtest/login.aspx");
	}
</script>
<body>
<h3>Using Cookie Authentication</h3>
<form runat=server>
	<h3><asp:label id="Welcome" runat=server/></h3>
	<asp:button text="Signout" OnClick="Signout_Click" runat=server/>
</form>
</body>
</html>

