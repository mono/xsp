<%@ Language="C#" %>
<html>
<script runat=server>
	void Clicked (object o, EventArgs e)
	{
		if (!Page.IsValid)
			return;

		int val;
		try {
			val = Int32.Parse (degrees.Text);
		} catch (Exception) {
			result.Text = "<font color=\"red\">Invalid number in degrees</font>";
			return;
		}

		int kval;
		switch (fromScale.SelectedIndex) {
		case 0:
			kval = val + 373;
			break;
		case 1:
			kval = 373 + (val - 32) * 5 / 9;
			break;
		case 2:
			if (val < 0) {
				result.Text = "<font color=\"red\">Kelvin is not defined for negtive numbers</font>";
				return;
			}
			kval = val;
			break;
		default:
			result.Text = "<font color=\"red\">Invalid from index</font>";
			return;
		}

		switch (toScale.SelectedIndex) {
		case 0:
			kval -= 373;
			break;
		case 1:
			kval = ((kval - 373) * 9 / 5) + 32;
			break;
		case 2:
			// Do nothing
			break;
		default:
			result.Text = "<font color=\"red\">Invalid from index</font>";
			return;
		}

		result.Text = String.Format ("Converting {0} from {1} to {2} gives <b>{3}</b>",
								val,
								fromScale.Value,
								toScale.Value,
								kval);
	}
</script>
<head>
<title>Temperature conversion</title>
</head>
<body>
<h3>Temperature conversion</h3>
Choose to/from and enter degrees.
<form runat="server">
	<span>From: </span>
	<select id="fromScale" runat="server">
		<option selected="true">Celsius</option>
		<option>Farenheit</option>
		<option>Kelvin</option>
	</select>
	&nbsp;
	<span>Degrees: </span>
	<asp:TextBox id="degrees" Text="" runat="server" />
	&nbsp;
	<span>To: </span>
	<select id="toScale" runat="server">
		<option>Celsius</option>
		<option selected="true">Farenheit</option>
		<option>Kelvin</option>
	</select>
	<p>
	<asp:Label runat=server id="result" Text="" />
	<p>
	<asp:Button id="btn"
	     Text="Calculate"
	     OnClick="Clicked"
	     runat="server"/>
	<p>	
	<asp:RequiredFieldValidator runat="server" id="vDegrees"
	     ControlToValidate="degrees" display="Dynamic"
	     ErrorMessage="Degrees not filled!" />
</form>
</body>
</html>
