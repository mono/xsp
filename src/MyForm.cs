//
// MyForm.cs: override some methods of HtmlForm to make it work for us when
// 	      rendering forms.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Licensed under the terms of the GNU GPL
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//

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
		writer.WriteAttribute ("name", Name);
		writer.WriteAttribute ("method", Method);
		writer.WriteAttribute ("action", "DummyAction.aspx", true);
		if (this.Enctype != null){
			writer.WriteAttribute ("enctype", Enctype);
			Attributes.Remove ("enctype");
		}
	}

	protected override void RenderChildren (HtmlTextWriter writer)
	{
		foreach (Control c in Controls)
			c.RenderControl (writer);
	}

}
}

