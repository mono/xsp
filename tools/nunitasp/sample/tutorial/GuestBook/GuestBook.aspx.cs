using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace GuestBook
{
	public class GuestBook : System.Web.UI.Page
	{
		protected System.Web.UI.WebControls.TextBox name;
		protected System.Web.UI.WebControls.DataGrid book;
		protected System.Web.UI.WebControls.Button save;
		protected System.Web.UI.WebControls.TextBox comments;
	
		private void Page_Load(object sender, System.EventArgs e)
		{
		}

		protected void Save_Clicked(object sender, EventArgs e)
		{
			DataTable table = (DataTable)Session["GuestBookData"];
			if (table == null) 
			{
				table = new DataTable();
				table.Columns.Add(new DataColumn("Name", typeof(string)));
				table.Columns.Add(new DataColumn("Comments", typeof(string)));
			}

			DataRow row = table.NewRow();
			row["Name"] = name.Text;
			row["Comments"] = comments.Text;
			table.Rows.Add(row);

			book.DataSource = table;
			book.DataBind();

			Session["GuestBookData"] = table;
			name.Text = "";
			comments.Text = "";
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
			this.Load += new System.EventHandler(this.Page_Load);

		}
		#endregion
	}
}
