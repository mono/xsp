<%@ Page Language="C#" %>
<html>
<head>
<script language="C#" runat="server">
	void Page_Load(object src, EventArgs e)
	{
		if (!Page.IsPostBack){

			ArrayList values = new ArrayList();

			values.Add (0);
			values.Add (1);
			values.Add (2);
			values.Add (3);
			values.Add (4);
			values.Add (5);
			values.Add (6);

			DataList1.DataSource = values;
			DataList1.DataBind();
		}
	}

	string EvenOrOdd(int number)
	{
		return (string) ((number % 2) == 0 ? "even" : "odd");
	}
</script>
</head>
<body>
<h3>Data binding and templates</h3>
	Testing data bound literal inside templates.
	<form runat=server>
		<asp:DataList id="DataList1" runat="server"
			BorderColor="blue"
			BorderWidth="2"
			GridLines="Both"
			CellPadding="5"
			CellSpacing="2" >

			<ItemTemplate>
			Number: <%# Container.DataItem %>
			This is an <b><%# EvenOrOdd((int) Container.DataItem) %></b> number.
			</ItemTemplate>
		</asp:datalist>
	</form>
</body>
</html>


