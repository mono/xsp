using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;

namespace Samples
{
	class SiteMapNode
	{
		XmlNode myNode;
		
		SiteMapNode[] children;
		SiteMapNode parent;
		string title;
		string url;

		public string Url {
			get { return url; }
		}

		public string Title {
			get { return title; }
		}

		public SiteMapNode Parent {
			get { return parent; }
		}
		
		public SiteMapNode (XmlNode node) : this (node, null)
		{
		}
		
		public SiteMapNode (XmlNode node, SiteMapNode parent)
		{
			if (node == null)
				return;
			
			this.parent = parent;
			myNode = node;
			title = GetAttribute ("title");
			url = GetAttribute ("url");

			if (!node.HasChildNodes)
				return;

			children = new SiteMapNode [node.ChildNodes.Count];

			int i = 0;
			foreach (XmlNode child in node.ChildNodes) {
				children [i] = new SiteMapNode (child, this);
				i++;
			}
		}

		string GetFileClass (string path)
		{
			if (path == null || path.Length == 0)
				return String.Empty;

			string ext = Path.GetExtension (path);
			if (ext == null || ext.Length == 0)
				return String.Empty;
			
			return "," + ext.Substring (1);
		}
		
		public string RenderNavigationList ()
		{
			if (Url.StartsWith ("~/2.0"))
				return String.Empty;
			
			StringBuilder sb = new StringBuilder ();

			if (parent == null)
				sb.Append ("<ul class=\"dirlist\">");

			string absPath = ToAbsolute (Url);
			sb.AppendFormat ("<li><a class=\"SampleLink{2}\" href=\"{0}\">{1}</a>",
					 absPath, Title, GetFileClass (absPath));
			
			if (children != null && children.Length > 0) {
				sb.Append ("<ul>");
				foreach (SiteMapNode child in children)
					sb.Append (child.RenderNavigationList ());
				sb.Append ("</ul>");
			}
			
			if (parent == null)
				sb.Append ("</ul>");

			return sb.ToString ();
		}

		public void BuildNodeMap (Hashtable table)
		{
			if (url != null && url.Length > 0) {
				string absoluteUrl = ToAbsolute (url);
				if (absoluteUrl != null && table [absoluteUrl] == null)
					table [absoluteUrl] = this;
			}
			
			if (children == null || children.Length == 0)
				return;

			foreach (SiteMapNode child in children)
				child.BuildNodeMap (table);
		}

		public string RenderBreadCrumbs ()
		{
			if (parent == null)
				return String.Empty;

			ArrayList al = new ArrayList ();

			SiteMapNode cur = this;
			while (cur != null) {
				if (cur == this)
					al.Insert (0, String.Format ("<span class=\"CrumbsCurrent\">{0}</span>",
								     cur.Title));
				else
					al.Insert (0, String.Format ("<a class=\"CrumbsParent\" href=\"{0}\">{1}</a>",
								     ToAbsolute (cur.Url), cur.Title));
				cur = cur.Parent;
			}

			string[] crumbs = (string[])al.ToArray (typeof (string));
			return String.Join (" -&gt; ", crumbs);
		}
		
		static bool IsAppRelative (string virtualPath)
		{
			if (virtualPath == null)
				return false;

			if (virtualPath.Length == 1 && virtualPath [0] == '~')
				return true;

			if (virtualPath [0] == '~' && (virtualPath [1] == '/'))
				return true;

			return false;
		}
		
		public static string ToAbsolute (string virtualPath)
		{
			int vplen;
			
			if (virtualPath == null || (vplen = virtualPath.Length) == 0)
				return null;
			
			string apppath = HttpRuntime.AppDomainAppVirtualPath;
			if (apppath == null)
				return null;

			if (IsAppRelative (virtualPath)) {
				if (apppath [0] != '/')
					return null;

				return apppath + (vplen == 1 ? "/" : virtualPath.Substring (vplen > 1 ? 2 : 1));
			}

			if (virtualPath [0] != '/')
				return null;

			return virtualPath;
		}
		
		string GetAttribute (string name)
		{
			XmlAttribute attr = myNode.Attributes [name];
			if (attr == null)
				return null;
			return attr.Value;
		}
	}
	
	public class SiteMapReader
	{
		Hashtable pathToNode;
		SiteMapNode root;
		
		public SiteMapReader (string filePath)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (filePath);

			if (doc.DocumentElement.LocalName != "siteMap")
				return;

			root = new SiteMapNode (doc.DocumentElement.FirstChild);
		}

		public string RenderNavigationList ()
		{
			if (root == null)
				return String.Empty;
			
			return root.RenderNavigationList ();
		}

		public string RenderBreadCrumbs (HttpContext context)
		{
			HttpRequest req;
			
			if (context == null || (req = context.Request) == null)
				return String.Empty;

			if (pathToNode == null) {
				pathToNode = new Hashtable ();
				root.BuildNodeMap (pathToNode);
			}

			SiteMapNode curNode = pathToNode [SiteMapNode.ToAbsolute (req.RawUrl)] as SiteMapNode;
			if (curNode == null)
				return String.Empty;
			
			string ret = curNode.RenderBreadCrumbs ();

			return ret;
		}
	}
}
