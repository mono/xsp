//
// tabcontrol.cs: sample user control.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Licensed under the terms of the GNU GPL
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
	public class Tabs2 : UserControl, IPostBackEventHandler
	{
		Hashtable tabData;
		StateBag localValues;
		ArrayList titles;

		public Tabs2 ()
		{
			titles = new ArrayList ();
			localValues = new StateBag (false);
		}

		public void AddTab (string title, ControlCollection controls)
		{
			if (title == null || title == String.Empty || controls == null)
				return;

			if (tabData == null) {
				tabData = new Hashtable ();
				CurrentTabName = title;
			}

			tabData.Add (title, controls);
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
				Triplet t = new Triplet (tabData, localValues, titles);
				return new Pair (base.SaveViewState (), t);
			}
			return null;
		}
		
		protected override void LoadViewState (object savedState)
		{
			if (savedState != null) {
				Pair saved = (Pair) savedState;
				base.LoadViewState (saved.First);
				Triplet t = (Triplet) saved.Second;
				tabData = t.First as Hashtable;
				localValues = t.Second as StateBag;
				titles = t.Third as ArrayList;
			}
		}
		
		protected override void OnPreRender (EventArgs e)
		{
			base.OnPreRender (e);
			Page.GetPostBackEventReference (this);
		}

		void IPostBackEventHandler.RaisePostBackEvent (string argument)
		{
			if (argument == null)
				return;

			if (CurrentTabName != argument)
				CurrentTabName = argument;
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
					string postbackEvent = String.Empty;
					if (Page != null)
						postbackEvent = Page.GetPostBackClientHyperlink (
									this, key);

					writer.WriteAttribute ("href", postbackEvent);
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
			Controls.Clear ();
			ControlCollection currentSet = (ControlCollection) tabData [CurrentTabName];
			foreach (Control ctrl in currentSet)
				Controls.Add (ctrl);
			base.RenderChildren (writer);
		}

		public int BlankWidth
		{
			get { 
				object o = localValues ["BlankWidth"];
				if (o == null)
					return 15;
				return (int) o;
			}
			set {
				localValues ["BlankWidth"] = value;
			}
		}

		public int Width
		{
			get { 
				object o = localValues ["Width"];
				if (o == null)
					return 120;
				return (int) o;
			}
			set {
				localValues ["Width"] = value;
			}
		}

		public string Align
		{
			get { 
				object o = localValues ["Align"];
				if (o == null)
					return "center";
				return (string) o;
			}
			set {
				localValues ["Align"] = value;
			}
		}

		public string CurrentTabName
		{
			get {
				object o = localValues ["CurrentTabName"];
				if (o == null)
					return String.Empty;
				return (string) localValues ["CurrentTabName"];
			}

			set {
				localValues ["CurrentTabName"] = value;
			}
		}

		public string CurrentTabColor
		{
			get {
				object o = localValues ["CurrentTabColor"];
				if (o == null)
					return "#FFFFFF";
				return (string) localValues ["CurrentTabColor"];
			}

			set {
				localValues ["CurrentTabColor"] = value;
			}
		}

		public string CurrentTabBackColor
		{
			get {
				object o = localValues ["CurrentTabBackColor"];
				if (o == null)
					return "#3366CC";
				return (string) localValues ["CurrentTabBackColor"];
			}

			set {
				localValues ["CurrentTabBackColor"] = value;
			}
		}

		public string TabBackColor
		{
			get {
				object o = localValues ["TabBackColor"];
				if (o == null)
					return "#efefef";
				return (string) localValues ["TabBackColor"];
			}

			set {
				localValues ["TabBackColor"] = value;
			}
		}
	}
}

