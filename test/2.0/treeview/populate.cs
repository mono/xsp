using System;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

public class Populate: Page
{
    public void TreeView1_TreeNodePopulate(object sender, TreeNodeEventArgs e)
    {
        if (e.Node.Depth > 4)
			return;

        for (int n = 0; n < (e.Node.Depth+1) * 2; n++)
        {
            TreeNode nod = new TreeNode("Node " + e.Node.Depth + " " + n);
            nod.PopulateOnDemand = true;
            e.Node.ChildNodes.Add(nod);
        }
    }

}
