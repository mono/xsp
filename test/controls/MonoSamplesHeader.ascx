<%@ Control Language="C#" %>
<script runat="server">
  static bool v2 = Environment.Version.Major == 2;

  void Page_Init (object sender, EventArgs args)
  {
      Control crumbs;
      if (v2)
          crumbs = LoadControl ("~/controls/BreadCrumbs_2.0.ascx");
      else
          crumbs = LoadControl ("~/controls/BreadCrumbs_1.1.ascx");

      BreadCrumbs.Controls.Clear ();
      BreadCrumbs.Controls.Add (crumbs);
  }
</script>
<div class="header">
  <a style="float:left; padding-right: 20px" class="header" href="http://mono-project.com/"><img src="/monobutton.png" alt="Mono site"></a>
  <h1 class="header">Welcome to Mono XSP!</h1>
  <div>    <h2 class="header">XSP is a simple web server written in C# that can be used to run your ASP.NET 
<% if (v2) 
      Response.Write ("2.0");
   else
      Response.Write ("1.1");
%> applications
    </h2>
  </div>
  <asp:PlaceHolder runat="server" id="BreadCrumbs" />
</div>
