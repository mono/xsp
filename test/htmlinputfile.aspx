<%@ Page Language="C#" %>
<html>
<title>HtmlInputFile</title>
<body>
<form id="myForm" name="myform" action="htmlinputfile.aspx" method="post" enctype="image/jpeg" runat="server">
Pick a JPEG file:
<input id="myFile" type="file" runat="server"> 
<br>
<input id="smt" type="submit" value="Go send it!" runat="server">
</form>
</body>
</html>

