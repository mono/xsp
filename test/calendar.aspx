<%@ Page Language = "C#" %>
<html>
<head>
<title>Testing properties in inner tags</title>
</head>
<body>
	<form runat=server>
		<h3>Calendar and properties</h3>
		<asp:calendar id="Calendar1"
		Font-Name="Arial" showtitle="true"
		runat="server">
			<SelectedDayStyle BackColor="Blue" 
					ForeColor="Red"/>
			<TodayDayStyle BackColor="#CCAACC" 
					ForeColor="#000000"/>
		</asp:Calendar>

	</form>
</body>
</html>

