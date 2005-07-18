<%@ language="C#" %>
<html>
<script runat=server>
        void Clicked (object o, EventArgs e)
        {
                Console.WriteLine (HttpContext.Current.Server.MapPath
("site.config"));
        }
</script>
<head>
<title>Button</title>
</head>
<body>
<form runat="server">
<asp:Button id="btn"
     Text="Submit"
     OnClick="Clicked"
     runat="server"/>
</form>
</body>
</html>
