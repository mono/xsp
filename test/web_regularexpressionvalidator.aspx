<%@ language="C#" %>
<html>
<script runat="server">
void Check_Click(Object src, EventArgs E) {
	message.Text = "Entered data is " + (Page.IsValid ? "valid." : "invalid!");
}
</script>

<head>
<title>RegularExpressionValidator</title>
</head>

<body>
<form runat="server">
<asp:Label text="Year (1900-2099, 2 or 4 digits):" runat="server"/><br>
<asp:TextBox id="year" runat="server" columns="4" maxLength="4"/><br>
<asp:RegularExpressionValidator runat="server"
     ControlToValidate="year"
     ValidationExpression="(19|20)?\d{2}"
     ErrorMessage="invalid!"/><br>
<br>

<asp:Label text="US Zip Code (xxxxx or xxxxx-xxxx):" runat="server"/><br>
<asp:TextBox id="zipcode" runat="server" columns="10" maxLength="10"/><br>
<asp:RegularExpressionValidator runat="server"
     ControlToValidate="zipcode"
     ValidationExpression="\d{5}(-\d{4})?"
     ErrorMessage="invalid format!"/><br>
<br>

<asp:Label text="Email address (e.g. someone@somewhere.com):" runat="server"/><br>
<asp:TextBox id="email" runat="server" columns="40"/><br>
<asp:RegularExpressionValidator runat="server"
     ControlToValidate="email"
     ValidationExpression="[\w\.\-]+@[a-zA-Z0-9\-]+(\.[a-zA-Z0-9\-]+)*(\.[a-zA-Z]{2,3}){1,2}"
     ErrorMessage="invalid format!"/><br>
<br>

<asp:Button text="Check" onclick="Check_Click" runat="server"/><br>
<br>
<asp:Label id="message" runat="server"/>
</form>
</body>
</html>

