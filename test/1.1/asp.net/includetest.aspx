<!-- #include virtual="header.inc" -->
<script language="C#" runat="server">
      void Clicked (object sender, EventArgs e)
      {
          One.Text = "Message text changed!";
          One.Color = "red";
          Two.Text = "Message text changed2!";
          Two.Color = "red";
	  Three.Text = "Text changed!";
      }
</script>

<body>
This is pretty much the same as registertest.aspx, but splitted in 3 files.
<p>
<!-- #include virtual="body.inc" -->
</body>
</html>

