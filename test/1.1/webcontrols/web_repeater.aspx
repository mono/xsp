<%@ Page Language="C#" %>
<html>
<head>
<title>Data bound Repeater</title>
<script runat="server">
	void Page_Load (object o, EventArgs e) 
	{
		if (!IsPostBack) {
			ArrayList list = new ArrayList ();
			list.Add (new Datum ("Spain", "es", "Europe"));
			list.Add (new Datum ("Japan", "jp", "Asia"));
			list.Add (new Datum ("Mexico", "mx", "America"));
			rep.DataSource = list;
			rep.DataBind ();
		}
	}

	public class Datum 
	{
		private string country;
		private string abbr;
		private string continent;

		public Datum (string country, string abbr, string continent)
		{
			this.country = country;
			this.abbr = abbr;
			this.continent = continent;
		}

		public string Country 
		{
			get { return country; }
		}

		public string Abbr 
		{
			get { return abbr; }
		}

		public string Continent 
		{
			get { return continent; }
		}

		public override string ToString ()
		{
			return country + " " + abbr + " " + continent;
		}
	} 
</script>
</head>
<body>
<form runat="server">
<asp:Repeater id=rep runat="server">
	<HeaderTemplate>
	<table border=2>
		<tr>
		<td><b>Country</b></td> 
		<td><b>Abbreviation</b></td>
		<td><b>Continent</b></td>
		</tr>
	</HeaderTemplate>
	<ItemTemplate>
	<tr>
	<td> 
	<%# DataBinder.Eval (Container.DataItem, "Country") %> 
	</td>
	<td> 
	<%# DataBinder.Eval (Container.DataItem, "Abbr") %>
	</td>
	<td> 
	<%# DataBinder.Eval (Container.DataItem, "Continent") %>
	</td>
	</tr>
	</ItemTemplate>
	<FooterTemplate>
	</table>
	</FooterTemplate>
</asp:Repeater>
</form>
</html>
</body>

