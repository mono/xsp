<%@ Page language="c#" AutoEventWireup="true" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN" > 
<!-- bug52334: columns not tracking view state -->
<html>
  <head>
    <title>WebForm33</title>
    <meta name="GENERATOR" Content="Microsoft Visual Studio .NET 7.1">
    <meta name="CODE_LANGUAGE" Content="C#">
    <meta name=vs_defaultClientScript content="JavaScript">
    <meta name=vs_targetSchema content="http://schemas.microsoft.com/intellisense/ie5">
  </head>
  <body MS_POSITIONING="GridLayout">
	
    <form id="Form1" method="post" runat="server">
    	    <asp:Label id="Label1" runat="server" />
            <asp:Button id="Button1" runat="server" Text="PostBack"></asp:Button>
     </form>
	
  </body>
  <script language=C# runat=server>
  		private void Page_Load(object sender, System.EventArgs e)
		{
			  
			HtmlForm frm = (HtmlForm)FindControl("Form1");
			if( Page.IsPostBack)
			{
				Response.Write("PostBack Worked!!!");
			}
			DataGrid objDataGrid = new DataGrid();
			objDataGrid.ID = "objDataGrid";
			objDataGrid.AutoGenerateColumns = false;            
			BoundColumn objBoundColumn;
			objBoundColumn = new BoundColumn();
			objBoundColumn.HeaderText = "IntegerValue";
			objBoundColumn.DataField = "IntegerValue";
			objDataGrid.Columns.Add(objBoundColumn);
			objBoundColumn = new BoundColumn();
			objBoundColumn.HeaderText = "StringValue";
			objBoundColumn.DataField = "StringValue";
			objDataGrid.Columns.Add(objBoundColumn);
			objBoundColumn = new BoundColumn();
			objBoundColumn.HeaderText = "CurrencyValue";
			objBoundColumn.DataField = "CurrencyValue";
			objDataGrid.Columns.Add(objBoundColumn);

			ButtonColumn objButtonColumn = new ButtonColumn();
			objButtonColumn.HeaderText = "ButtonColumn";
			objDataGrid.Columns.Add(objButtonColumn);

			EditCommandColumn objEditCommandColumn = new EditCommandColumn();
			objEditCommandColumn.HeaderText = "EditCommandColumn";
			objDataGrid.Columns.Add(objEditCommandColumn);


			objDataGrid.DataSource = CreateDataSource();
			objDataGrid.DataBind();
			frm.Controls.Add(objDataGrid);

			ButtonColumn objButtonColumnDataTextField;
			objButtonColumnDataTextField = (ButtonColumn)objDataGrid.Columns[3];
			if (!Page.IsPostBack)
				objButtonColumnDataTextField.DataTextField = "test";             

			Label1.Text = "field:" + objButtonColumnDataTextField.DataTextField;
		}

		private System.Data.DataTable CreateDataSource()
		{

			System.Data.DataTable dt = new System.Data.DataTable();
			System.Data.DataRow dr;

			dt.Columns.Add(new System.Data.DataColumn("IntegerValue", typeof(Int32)));
			dt.Columns.Add(new System.Data.DataColumn("StringValue", typeof(string)));
			dt.Columns.Add(new System.Data.DataColumn("CurrencyValue", typeof(Double)));

			for (int i = 0 ; i <= 8 ; i++)
			{
				dr = dt.NewRow();
				dr[0] = i;
				dr[1] = "Item " + i.ToString();
				dr[2] = 1.23 * (i + 1);
				dt.Rows.Add(dr);
			}
			return dt;
		}
  </script>
</html>
