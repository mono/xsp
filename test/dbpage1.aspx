<%@ language="C#" %>
<%@ import namespace="System.Configuration" %>
<%@ import namespace="System.Data" %>
<%@ import namespace="System.Reflection" %>

<html>
<script runat=server>

	static Type cncType = null;

	void GetConnectionData (out string providerAssembly, out string cncTypeName, out string cncString)
	{
		providerAssembly = null;
		cncTypeName = null;
		cncString = null;
		NameValueCollection config = ConfigurationSettings.AppSettings as NameValueCollection;
		if (config != null) {
			foreach (string s in config.Keys) {
				if (0 == String.Compare ("DBProviderAssembly", s, true)) {
					providerAssembly = config [s];
				} else if (0 == String.Compare ("DBConnectionType", s, true)) {
					cncTypeName = config [s];
				} else if (0 == String.Compare ("DBConnectionString", s, true)) {
					cncString = config [s];
				}
			}
		}

		if (providerAssembly == null || providerAssembly == "")
			providerAssembly = "Mono.Data.PostgreSqlClient";
		
		if (cncTypeName == null || cncTypeName == "")
			cncTypeName = "Mono.Data.PostgreSqlClient.PgSqlConnection";
		
		if (cncString == null || cncString == "")
			cncString = "hostaddr=127.0.0.1;user=monotest;password=monotest;dbname=monotest";
	}

	void ShowError (Exception exc)
	{
		noDBLine.InnerHtml += "<p><b>The error was:</b>\n<pre> " + exc + "</pre><p>";
		theForm.Visible = false;
		noDBLine.Visible = true;
	}

	IDbConnection cnc;
	void Page_Init (object sender, EventArgs e)
	{
		string connectionTypeName;
		string providerAssemblyName;
		string cncString;

		GetConnectionData (out providerAssemblyName, out connectionTypeName, out cncString);
		if (cncType == null) {		
			Assembly dbAssembly = Assembly.Load (providerAssemblyName);
			cncType = dbAssembly.GetType (connectionTypeName, true);
			if (!typeof (IDbConnection).IsAssignableFrom (cncType))
				throw new ApplicationException ("The type '" + cncType +
								"' does not implement IDbConnection.\n" +
								"Check 'DbConnectionType' in server.exe.config.");
		}

		cnc = (IDbConnection) Activator.CreateInstance (cncType);
		cnc.ConnectionString = cncString;
		try {
			cnc.Open ();
		} catch (Exception exc) {
			ShowError (exc);
			cnc = null;
		}
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

	void UpdateTable (string filterPerson, string filterMail)
	{
		if (cnc == null)
			return;

		IDbCommand selectCommand = cnc.CreateCommand();
		IDataReader reader;

		string selectCmd = "SELECT * FROM test " + 
				   "WHERE person like '" + filterPerson  + "' AND " +
					 "email like '" + filterMail + "'";

		selectCommand.CommandText = selectCmd;
		try {
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
		} catch (Exception exc) {
			ShowError (exc);
		}
	}

</script>
<head>
<title>Some DB testing</title>
</head>
<body>
<span runat="server" visible="false" id="noDBLine">
<h3>Database Error</h3>
Sorry, a database error has occurred.
<p>
You should set up a database for user <i>'monotest'</i>,
password <i>'monotest'</i> and dbname <i>'monotest'</i>.
<p>
Then modify the variables DBProviderAssembly, DBConnectionType and
DBConnectionString in server.exe.config file to fit your needs.
<p>
The database should have a table called customers created with the following command (or similar):
<pre>
CREATE TABLE "test" (
	"person" character varying(256) NOT NULL,
	"email" character varying(256) NOT NULL
);

</pre>
</span>
<form id="theForm" runat="server">
Choose the SQL filters and click 'Submit'.
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

