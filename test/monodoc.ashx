<%@ WebHandler Language="c#" class="Mono.Website.Handlers.MonodocHandler" %>
<%@ Assembly name="monodoc" %>

//
// Mono.Web.Handlers.MonodocHandler.  
//
// Authors:
//     Ben Maurer (bmaurer@users.sourceforge.net)
//
// (C) 2003 Ben Maurer
//

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Xml;
using System.Xml.Xsl;
using Monodoc;

namespace Mono.Website.Handlers
{
       public class MonodocHandler : IHttpHandler
       {
               static RootTree help_tree;
               static MonodocHandler ()
               {
                       help_tree = RootTree.LoadTree ();
               }

               void IHttpHandler.ProcessRequest (HttpContext context)
               {
                       string link = (string) context.Request.Params["link"];
                       if (link == null)
                               link = "root:";
                       
                       if (link.StartsWith ("source-id:") && (link.EndsWith (".gif") || link.EndsWith (".jpeg") || link.EndsWith (".jpg")  || link.EndsWith(".png")))
                       {
                               switch (link.Substring (link.LastIndexOf ('.') + 1))
                               {
                                       case "gif":
                                               context.Response.ContentType = "image/gif";
                                               break;
                                       case "jpeg":
                                       case "jpg":
                                               context.Response.ContentType = "image/jpeg";
                                               break;
                                       case "png":
                                               context.Response.ContentType = "image/png";
                                               break;
                                       default:
                                               throw new Exception ("Internal error");
                               }
                               
                               Stream s = help_tree.GetImage (link);
                               
                               if (s == null)
                                       throw new HttpException (404, "File not found");
                               
                               Copy (s, context.Response.OutputStream);
                               return;
                       }
                       
                       PrintDocs (link, context);
               }
               
               
               
               void Copy (Stream input, Stream output)
               {
                       const int BUFFER_SIZE=8192; // 8k buf
                       byte [] buffer = new byte [BUFFER_SIZE];
               
                       int len;
                       while ( (len = input.Read (buffer, 0, BUFFER_SIZE)) > 0)
                               output.Write (buffer, 0, len);
                       
                       output.Flush();
               }
               
               void PrintDocs (string url, HttpContext ctx)
               {
                       Node n;
                       
                       ctx.Response.Write (@"
<html>
<head>
<script>
<!--
function load ()
{
	objs = document.getElementsByTagName('a');
	for (i = 0; i < objs.length; i++) {
		e = objs [i];
		if (e.href == null) continue;
		
		objs[i].href = makeLink (objs[i].href);
	}
	
	objs = document.getElementsByTagName('img');
	for (i = 0; i < objs.length; i++)
	{
		e = objs [i];
		if (e.src == null) continue;
		
		objs[i].src = makeLink (objs[i].src);
	}
}

function makeLink (link)
{
	if (link == '') return '';
	if (link.charAt(0) == '#') return link;
	
	protocol = link.substring (0, link.indexOf (':'));
	switch (protocol)
	{
		case 'http':
		case 'ftp':
		case 'mailto':
			return link;
			
		default:
			return '" + ctx.Request.Path + @"?link=' + link.replace(/\+/g, '%2B');
	}
}
-->
</script>
<title>Mono Documentation</title>
</head>
<body onLoad='load()'>

                       ");
                       
                       ctx.Response.Write (help_tree.RenderUrl (url, out n));
               
                       ctx.Response.Write (@"
                       </body>
</html>");
               }

               bool IHttpHandler.IsReusable
               {
                       get {
                               return true;
                       }
               }

       }
}
