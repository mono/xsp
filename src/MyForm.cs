using System;
using System.Web.UI;

namespace Mono.ASP {
	
public class MyHtmlForm : System.Web.UI.HtmlControls.HtmlForm
{
	public MyHtmlForm() 
	{
	}
				
	protected override void RenderAttributes (HtmlTextWriter writer){
		writer.WriteAttribute ("id", ID);
		//FIXME
		writer.WriteAttribute ("method", "post");
		//FIXME
		writer.WriteAttribute ("action", "DummyAction.aspx", true);
	}

	protected override void RenderChildren (HtmlTextWriter writer)
	{
		foreach (Control c in Controls){
			Console.WriteLine ("Rendering {0} {1}", c.GetType (), c.ID);
			Console.WriteLine ("Parent: {0}", c.Parent.ID);
			Console.WriteLine ("Page: {0}", c.Page.ID);
			c.RenderControl (writer);
		}
	}

}
}

