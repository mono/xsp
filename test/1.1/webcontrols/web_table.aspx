<%@ Page Language="C#" %>
<html>
<head>
<title>HtmlTable, HtmlTableRow, HtmlTableCell</title>
<script runat="server">
	void Page_Load(Object sender, EventArgs e) 
	{
		for (int i = 0; i < 5; i++){
			TableRow row = new TableRow ();
			for (int j = 0; j < 4; j++){
				TableCell cell = new TableCell ();
				cell.Controls.Add (new LiteralControl ("Row " + i + ", cell " + j));
				row.Cells.Add (cell);
			}
			myTable.Rows.Add (row);
		}
	}
</script>
</head>
<body>  
<form runat="server">
<asp:Table id="myTable" HorizontalAlign="Center" Font-Size="12pt" GridLines="both" 
CellPadding="5" runat="server"/>
</form>
</body>
</html>

