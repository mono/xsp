<%@ Page Language="C#" %>
<html>
	<body>
		<form runat="server">
		<asp:XmlDataSource
			runat="server"
			id="xds"
			DataFile="data.xml"
			XPath="/people/person"
		/>
			
			<asp:Repeater
				runat="server"
				DataSourceID="xds"
			>
				
				<ItemTemplate>
					<h2><%# XPath ("name") %></h2>
					
					<ul>
						<asp:Repeater
							DataSource='<%# XPathSelect ("jobs/job") %>'
							runat="server"
						>
							<ItemTemplate>
								<li><%# XPath ("string (.)") %></li>
							</ItemTemplate>
						</asp:Repeater>
					</ul>
				</ItemTemplate>
			</asp:Repeater>
		</form>
	</body>
</html>