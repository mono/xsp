<%@ Page Language="C#" %>
<html>
<title>Panel with a HyperLink</title>
<body>
<form runat="server">
<asp:Panel id="pan" runat="server" ForeColor="green" Height="100px" Width="150px">
<asp:HyperLink id="hyper" NavigateUrl="http://www.go-mono.com"
Text="Mono site" Target="_top" runat="server"/>
<p>
</asp:Panel>
</form>
</body>
</html>

