<%@ language="C#"%>
<%@ Import namespace="System.IO" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN">
<html>
<head>
<title>Welcome to Mono XSP!</title>
<link href="favicon.ico" rel="SHORTCUT ICON" />
<link rel="stylesheet" type="text/css" href="mono-xsp.css">
<meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1">
<script runat="server">

	protected override void OnLoad (EventArgs args)
	{
		base.OnLoad (args);
		ReadDirectory (Path.GetDirectoryName (Request.PhysicalPath), "", TreeView1.Nodes);
	}
	
	public void ReadDirectory (string path, string basePath, TreeNodeCollection nodes)
	{
		foreach (string sdir in Directory.GetDirectories (path)) {
			TreeNode node = new TreeNode ("<b>" + Path.GetFileName (sdir) + "</b>");
			node.SelectAction = TreeNodeSelectAction.Expand;
			ReadDirectory (sdir, basePath + Path.GetFileName (sdir) + "/", node.ChildNodes);
			if (node.ChildNodes.Count > 0)
				nodes.Add (node);
		}
		foreach (string file in Directory.GetFiles (path)) {
			string fileName = basePath + Path.GetFileName (file);
			if (fileName == "index.aspx" || fileName == "index2.aspx") continue;
			string extension = Path.GetExtension (file);
			if (extension == ".aspx" || extension == ".ashx" || extension == ".asmx") {
				TreeNode node = new TreeNode ("&nbsp;" + Path.GetFileName (fileName));
				node.NavigateUrl = fileName;
				node.ImageUrl = "small-icon.png";
				nodes.Add (node);
			}
		}
	}
	
</script>
</head>
<body>
<div class="header">
<a style="float:left; padding-right: 20px" class="header" href="http://www.go-mono.com"><img src="monobutton.png" alt="Mono site"></a>
<h1 class="header">
Welcome to Mono XSP!
</h1>
<div>
<h2 class="header">XSP is a simple web server written in C# that can be used to run your ASP.NET 2.0 applications
</h2>
</div>
</div>

<p><table width=100%>
<tr valign="top">
<td>Here are some ASP.NET examples:

<form id="form1" runat="server">
    <asp:TreeView style="margin:10px" ID="TreeView1" Runat="server"
        EnableClientScript="true"
        PopulateNodesFromClient="false"  
        ExpandDepth="1"
        >
     </asp:TreeView>
</form>
</td>
<td><p align="right"><img style="float:right" src="mono-powered-big.png" alt="Mono Powered"></p></td>
</tr></table>
</p>
</html>

