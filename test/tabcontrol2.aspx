<%@ Page Language="C#" %>
<%@ Import namespace="System.Reflection" %>
<%@ Register TagPrefix="Mono" Namespace="Mono.Controls" assembly="tabcontrol2.dll" %>
<html>
<!-- You must compile tabcontrol2.cs and copy the dll to the output/ directory -->
<!-- Authors:
--	Gonzalo Paniagua Javier (gonzalo@ximian.com)
--	(c) 2002 Ximian, Inc (http://www.ximian.com)
-->

<title>User Control tabcontrol2</title>
<script runat=server>
	private static int _clicked = 0;
	void Clicked (object o, EventArgs e)
	{
		uno.InnerText = String.Format ("Somebody pressed me {0} times.", ++_clicked);
	}

	private static int _txt_changed = 0;
	void txt_Changed (object sender, EventArgs e)
	{
		dos.InnerText = String.Format ("Text have changed {0} times.", ++_txt_changed);
	}
</script>

<body>
    <center>
        <h3>Test for Tabs user control (tabcontrol2.dll)</h3>
        <hr>
    </center>

    <form runat="server">
        <Mono:Tabs2 runat="server" id="tabs">
    	    <Mono:TabContent runat="server" label="Empty" />
    	    <Mono:TabContent runat="server" label="Image">
        	Hi there
        	<p>
        	<asp:Image id="im" runat="server"
        		AlternateText="Yes, again the dancing monkey"
        		ImageAlign="left"
        		ImageUrl="http://www.ximian.com/images/logo_ximian.gif"/>
            </Mono:TabContent>
            <Mono:TabContent runat="server" Label="Form">
        	 <asp:Button id="btn"
        		Text="Submit"
        		OnClick="Clicked"
        		runat="server"/>
        	 <br>
        	 <span runat=server id="uno"/>
        	 <br>
        	 <span runat=server id="dos"/>
        	 <br>
        	 <asp:TextBox id="txt1" Text="You can write here." TextMode="MultiLine" 
		  OnTextChanged="txt_Changed" runat="server" rows=5 />
        	 <br>
            </Mono:TabContent>
        </Mono:Tabs2>
    </form>
</body>
</html>

