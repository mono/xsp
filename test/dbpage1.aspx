<%@ language="C#" %>
<%@ import namespace="System" %>
<%@ import namespace="System.Data" %>
<%@ import namespace="System.Reflection" %>

<html>
<script runat=server>

	// FIXME: temporary hack to get this working
	static Assembly dbAssembly = null;
	static Type typ = null;

	/* You must setup a user (monotest, pw=monotest), a database
	 *called monotest with a table called test which has two columns:
	 *person and email
	 */
	
	IDbConnection cnc;
	void Page_Init (object sender, EventArgs e)
	{
		// FIXME: temporary hack to get this working until
		// we can use global.asax file (no support fot it yet)
		// to dynamically load an assembly
		if(dbAssembly == null) {		
			const string connectionTypeName = "Mono.Data.PostgreSqlClient.PgSqlConnection";
			const string providerAssemblyName = "Mono.Data.PostgreSqlClient";			
			dbAssembly = Assembly.Load (providerAssemblyName);
			typ = dbAssembly.GetType (connectionTypeName);
		}

		cnc = (IDbConnection) Activator.CreateInstance (typ);
		
		string connectionString = "hostaddr=127.0.0.1;" +
					  "user=monotest;" +
					  "password=monotest;" +
					  "dbname=monotest";

		cnc.ConnectionString = connectionString;
		cnc.Open ();
	}

	void Page_Load (object sender, EventArgs e)
	{
		if (!IsPostBack){
			PersonFilter.Text = "%";
			MailFilter.Text = "%";
			UpdateTable (PersonFilter.Text, MailFilter.Text);
		}
	}

	void Filter_Changed (object sender, EventArgs e)
	{
		UpdateTable (PersonFilter.Text, MailFilter.Text);
		
	}

	private void UpdateTable (string filterPerson, string filterMail)
	{
		IDbCommand selectCommand = cnc.CreateCommand();
		IDataReader reader;

		string selectCmd = "SELECT * FROM test " + 
				   "WHERE person like '" + filterPerson  + "' AND " +
					 "email like '" + filterMail + "'";

		selectCommand.CommandText = selectCmd;
		reader = selectCommand.ExecuteReader ();
		while (reader.Read ()) {
			TableRow row = new TableRow ();
			for (int i = 0; i < reader.FieldCount; i++) {
				TableCell cell = new TableCell ();
				cell.Controls.Add (new LiteralControl (reader.GetValue (i).ToString ()));
				row.Cells.Add (cell);
			}
			myTable.Rows.Add (row);
			
		}
	}

</script>
<head>
<title>Some DB testing</title>
</head>
<body>
Choose the SQL filters and click 'Submit'.
<form runat="server">
<asp:Label Text="Person Filter: " />
<asp:TextBox id="PersonFilter" Text="" TextMode="singleLine" OnTextChanged="Filter_Changed" runat="server" maxlength=40 />
<p>
<asp:Label Text="Mail Filter: " />
<asp:TextBox id="MailFilter" Text="" TextMode="singleLine" OnTextChanged="Filter_Changed" runat="server" maxlength=40 visible=false />
<p>
<asp:Button id="btn" runat="server" Text="Submit" />
<p>
<asp:Table id="myTable" HorizontalAlign="Left" Font-Size="12pt" GridLines="both" 
CellPadding="5" runat="server"/>
</form>
</body>
</html>

