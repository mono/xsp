<%@ Page Language="C#" %>
<html>
<head>
<script runat="server">
	void Clicked (object o, ImageClickEventArgs e)
	{
		// e.X -> x coordinate of the click
		// e.Y -> y coordinate of the click
	}
</script>
<title>ImageButton</title>
</head>
<body>
<form runat="server">
<asp:ImageButton id="imgButton" AlternateText="Image button" 
OnClick="Clicked" ImageUrl="http://www.mono-project.com/files/0/08/Mono-powered.png" 
ImageAlign="left" runat="server"/>
</form>
</body>
</html>

