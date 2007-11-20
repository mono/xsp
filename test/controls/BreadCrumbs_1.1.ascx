<%@ Control Language="C#" %>
<%@ Assembly Name="SiteMapReader_1.1" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="Samples" %>
<%
  {
          SiteMapReader smr = Application ["SiteMapReaderInstance"] as SiteMapReader;
          if (smr != null)
                  crumbs.InnerHtml = smr.RenderBreadCrumbs (HttpContext.Current);
          else
                  crumbs.InnerHtml = "<strong>SiteMapReader instance not found</strong>";
  }
%>
<span runat="server" id="crumbs"></span>

