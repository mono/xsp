<%@ Page Language="C#" %>
<html>
<head>
<title>HtmlTable, HtmlTableRow, HtmlTableCell</title>
<script runat="server">
	void Page_Load(Object sender, EventArgs e) 
	{
		for (int i = 0; i < 5; i++){
			HtmlTableRow row = new HtmlTableRow ();
			for (int j = 0; j < 4; j++){
				HtmlTableCell cell = new HtmlTableCell ();
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
<p>
<table id="myTable" CellPadding=2 CellSpacing=1 Border="2" BorderColor="blue" runat="server" /> 
</form>
</body>
</html>

