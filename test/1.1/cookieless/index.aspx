<%@ Page Language="C#" %>
<%@ Register TagPrefix="mono" TagName="MonoSamplesHeader" src="~/controls/MonoSamplesHeader.ascx" %>
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
<link rel="stylesheet" type="text/css" href="/mono-xsp.css">
</head>
<body><mono:MonoSamplesHeader runat="server"/>
<form runat="server">
<asp:Button id="btn"
     Text="Submit"
     OnClick="Clicked"
     runat="server"/>
</form>
</body>
</html>
