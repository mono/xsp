<%@ PAGE LANGUAGE = C# %>
<%--
-- Test calling map path on a relative path. This should print
-- the full path of map-path.aspx
--%>
<html>
<script runat=server>
   void Page_Load()
   {
	Response.Write (Server.MapPath ("map-path.aspx"));
   }
</script>
<body>
</body>
</html>
