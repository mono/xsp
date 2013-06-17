<%@ language="C#"%>
<%@ Import namespace="System.IO" %>
<%@ Register TagPrefix="mono" TagName="MonoSamplesHeader" src="~/controls/MonoSamplesHeader.ascx" %>
<!DOCTYPE html>
<html>
<head>
    <meta charset=utf-8 />
    <title>Welcome to Mono XSP!</title>
    <link href="favicon.ico" rel="SHORTCUT ICON" />
    <link rel="stylesheet" type="text/css" href="/mono-xsp.css">
</head>
<body>
    <mono:MonoSamplesHeader runat="server"/>
    <table style="width: 100%">
        <tr style="vertical-align: top">
            <td>
                <form id="form1" runat="server">
                    <asp:SiteMapDataSource runat="server" id="SamplesSiteMap"/>
                    <asp:TreeView style="margin:10px" id="TreeView2" runat="server" DataSourceId="SamplesSiteMap"
                        EnableClientScript="true" PopulateNodesFromClient="false" ExpandDepth="2"/>
                </form>
            </td>
            <td>
                <p style="text-align: right">
                    <img style="float:right" src="mono-powered-big.png" alt="Mono Powered">
                </p>
            </td>
        </tr>
    </table>
</body>
</html>
