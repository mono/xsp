using System;
using System.Collections;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace MonoTest
{
	/// <summary>
	/// Summary description for Test.
	/// </summary>
	public class Test : System.Web.UI.Page
	{
		protected System.Web.UI.WebControls.Label Label1;
		protected System.Web.UI.WebControls.Button SubmitButton;
		protected System.Web.UI.WebControls.TextBox TextBox1;
	
		private void Page_Load(object sender, System.EventArgs e)
		{
			Label1.Text = "Page Loaded";
		}

		#region Web Form Designer generated code
		override protected void OnInit(EventArgs e)
		{
			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit(e);
		}
		
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{    
			this.SubmitButton.Click += new System.EventHandler(this.SubmitButton_Click);
			this.Load += new System.EventHandler(this.Page_Load);

		}
		#endregion

		private void SubmitButton_Click(object sender, System.EventArgs e)
		{
			Label1.Text = TextBox1.Text;
		}

	}
}
