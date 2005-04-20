<%@ Page Language="VB" Explicit="true" Strict="True" %>
<html>
<head>
	<script runat="server">
	Sub Button1_OnClick (Source As object, e As EventArgs) 
		If (Button1.InnerText = "Enabled 1") Then
			Span1.InnerHtml="You deactivated Button1"
			Button1.InnerText = "Disabled 1"
		Else
			Span1.InnerHtml="You activated Button1"
			Button1.InnerText = "Enabled 1"
		End If
	End Sub
	</script>
	<title>HtmlButton VB.NET Sample</title>
</head>
<body>
	<h3>HtmlButton VB.NET Sample</h3>
	<form id="ServerForm" runat="server">     
		<button id=Button1 runat="server" OnServerClick="Button1_OnClick">
		Button1
		</button>
		&nbsp;
		<span id=Span1 style="color:red" runat="server" />
	</form>
</body>
</html>

