//
// tabcontrol.cs: sample user control.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Licensed under the terms of the MIT X11 license
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//

using System;
using System.Collections;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace Mono.Controls
{
	public class Tabs : UserControl
	{
		Hashtable tabData;
		ArrayList titles;

		public Tabs ()
		{
			titles = new ArrayList ();
		}

		public void AddTab (string title, string url)
		{
			if (title == null || title == String.Empty || url == null || url == String.Empty)
				return;

			if (tabData == null) {
				tabData = new Hashtable ();
				CurrentTabName = title;
			}

			tabData.Add (title, url);
			titles.Add (title);
		}

		public void Clear ()
		{
			tabData = null;
			CurrentTabName = "";
		}
		
		public void RemoveTab (string title)
		{
			tabData.Remove (title);
		}

		protected override object SaveViewState ()
		{
			if (tabData != null) {
				Pair p = new Pair (tabData, titles);
				return new Pair (base.SaveViewState (), p);
			}
			return null;
		}
		
		protected override void LoadViewState (object savedState)
		{
			if (savedState != null) {
				Pair saved = (Pair) savedState;
				base.LoadViewState (saved.First);
				Pair p = (Pair) saved.Second;
				tabData = p.First as Hashtable;
				titles = p.Second as ArrayList;
			}
		}
		
		private void RenderBlank (HtmlTextWriter writer)
		{
			writer.WriteBeginTag ("td");
			writer.WriteAttribute ("bgcolor", TabBackColor);
			writer.WriteAttribute ("width", BlankWidth.ToString ());
			writer.Write (">");
			writer.Write ("&nbsp;");
			writer.WriteEndTag ("td");
		}

		private void RenderTabs (HtmlTextWriter writer)
		{
			writer.WriteBeginTag ("tr");
			writer.Write (">");
			writer.WriteLine ();

			if (titles.Count > 0)
				RenderBlank (writer);
			string currentTab = CurrentTabName;
			string key;
			int end = titles.Count;
			for (int i = 0; i < end; i++) {
				key = (string) titles [i];
				writer.WriteBeginTag ("td");
				writer.WriteAttribute ("width", Width.ToString ());
				writer.WriteAttribute ("align", Align.ToString ());
				if (key == currentTab) {
					writer.WriteAttribute ("bgcolor", CurrentTabBackColor);
					writer.Write (">");
					writer.WriteBeginTag ("font");
					writer.WriteAttribute ("color", CurrentTabColor);
					writer.Write (">");
					writer.Write (key);
					writer.WriteEndTag ("font");
				} else {
					writer.WriteAttribute ("bgcolor", TabBackColor);
					writer.Write (">");
					writer.WriteBeginTag ("a");
					writer.WriteAttribute ("href", tabData [key] as string);
					writer.Write (">");
					writer.Write (key);
					writer.WriteEndTag ("a");
				}
				writer.WriteEndTag ("td");
				RenderBlank (writer);
				writer.WriteLine ();
			}

			writer.WriteEndTag ("tr");
			writer.WriteBeginTag ("tr");
			writer.Write (">");
			writer.WriteLine ();
			writer.WriteBeginTag ("td");
			writer.WriteAttribute ("colspan", "10");
			writer.WriteAttribute ("bgcolor", CurrentTabBackColor);
			writer.Write (">");
			writer.WriteBeginTag ("img");
			writer.WriteAttribute ("width", "1");
			writer.WriteAttribute ("height", "1");
			writer.WriteAttribute ("alt", "");
			writer.Write (">");
			writer.WriteEndTag ("td");
			writer.WriteEndTag ("tr");
		}
		
		protected override void Render (HtmlTextWriter writer)
		{
			if (tabData == null || tabData.Count == 0)
				return;

			writer.WriteBeginTag ("table");
			writer.WriteAttribute ("border", "0");
			writer.WriteAttribute ("cellpadding", "0");
			writer.WriteAttribute ("cellspacing", "0");
			writer.Write (">");
			writer.WriteBeginTag ("tbody");
			writer.Write (">");
			writer.WriteLine ();
			RenderTabs (writer);
			writer.WriteEndTag ("tbody");
			writer.WriteEndTag ("table");
			writer.WriteLine ();
		}

		public int BlankWidth
		{
			get { 
				object o = ViewState ["BlankWidth"];
				if (o == null)
					return 15;
				return (int) o;
			}
			set {
				ViewState ["BlankWidth"] = value;
			}
		}

		public int Width
		{
			get { 
				object o = ViewState ["Width"];
				if (o == null)
					return 120;
				return (int) o;
			}
			set {
				ViewState ["Width"] = value;
			}
		}

		public string Align
		{
			get { 
				object o = ViewState ["Align"];
				if (o == null)
					return "center";
				return (string) o;
			}
			set {
				ViewState ["Align"] = value;
			}
		}

		public string CurrentTabName
		{
			get {
				object o = ViewState ["CurrentTabName"];
				if (o == null)
					return String.Empty;
				return (string) ViewState ["CurrentTabName"];
			}

			set {
				ViewState ["CurrentTabName"] = value;
			}
		}

		public string CurrentTabColor
		{
			get {
				object o = ViewState ["CurrentTabColor"];
				if (o == null)
					return "#FFFFFF";
				return (string) ViewState ["CurrentTabColor"];
			}

			set {
				ViewState ["CurrentTabColor"] = value;
			}
		}

		public string CurrentTabBackColor
		{
			get {
				object o = ViewState ["CurrentTabBackColor"];
				if (o == null)
					return "#3366CC";
				return (string) ViewState ["CurrentTabBackColor"];
			}

			set {
				ViewState ["CurrentTabBackColor"] = value;
			}
		}

		public string TabBackColor
		{
			get {
				object o = ViewState ["TabBackColor"];
				if (o == null)
					return "#efefef";
				return (string) ViewState ["TabBackColor"];
			}

			set {
				ViewState ["TabBackColor"] = value;
			}
		}
	}
}

