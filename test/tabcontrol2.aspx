<%@ Page Language="C#" %>
<%@ Import namespace="System.Reflection" %>
<%@ Register TagPrefix="Mono" NAmespace="Mono.Controls" assembly="tabcontrol2.dll" %>
<html>
<!-- You must compile tabcontrol.cs and copy the dll to the output/ directory -->
<!-- Authors:
--	Gonzalo Paniagua Javier (gonzalo@ximian.com)
--	(c) 2002 Ximian, Inc (http://www.ximian.com)
-->

<title>User Control 3</title>
<script runat="server">
	void Page_Load (object sender, EventArgs e)
	{
		if (!IsPostBack) {
			ControlCollection coll = new ControlCollection (tabs);
			tabs.AddTab ("Empty", coll);

			coll = new ControlCollection (tabs);
			coll.Add (new LiteralControl ("<br>Ok, this is just some text"));
			tabs.AddTab ("Some text", coll);

			coll = new ControlCollection (tabs);
			coll.Add (new LiteralControl ("<br>A TextBox: "));
			TextBox tb = new TextBox ();
			tb.ID = "tb1";
			coll.Add (tb);
			tabs.AddTab ("TextBox", coll);
			coll = new ControlCollection (tabs);
			coll.Add (new LiteralControl ("\n<br><p>And, well, also an image:"));
			Image img = new Image ();
			img.AlternateText = "Yes, again the dancing monkey";
			img.ImageUrl = "http://www.ximian.com/images/logo_ximian.gif";
			coll.Add (img);
			tabs.AddTab ("Image", coll);
		}

	}

</script>
<body>
<center>
<h3>Test for Tabs2 user control (tabcontrol2.dll)</h3>
<hr>
</center>
<form runat="server">
<Mono:Tabs2 runat="server" id="tabs"/>
</form>
</body>
</html>

