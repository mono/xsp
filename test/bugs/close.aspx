<%@ PAGE LANGUAGE = C# %>
<%--
-- Test closing Response.OutputStream and then writing again.
-- Should also be tested with gzip or any other response filter
-- enabled so that the stream is used after page.ProcessRequest
--%>
<html>
<script runat=server>
   void Page_Load()
   {
	Response.Write ("You should see a hello world by the end<br/>");
	Response.OutputStream.Close ();
	Response.Write ("Hey!<br/>");
	Response.Output.WriteLine ("Helloe world!<br/>");
   }
</script>
<body>
</body>
</html>
