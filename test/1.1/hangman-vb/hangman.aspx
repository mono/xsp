<%@ Page language="C#" Inherits="NS_HM.HM" %>

<html>
<body>
	<table width="100%">
		<tr bgColor="black" style="color:white;font-size:20pt">
			<td color="white">
	<center>
	Guess the Mono related Word.<br> <font size="22pt"> Or be prepared to be hanged ! </font>
	</center>
		</td> </tr> </table>

<form runat="server">
<center>
  <asp:image id="hangmanImage" runat="server" />
  <p>
       
  <asp:literal runat="server" id="currentWord" />
  <p>
  
  <asp:LinkButton runat="server" id="A" CommandArgument="A" Text="A" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="B" CommandArgument="B" Text="B" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="C" CommandArgument="C" Text="C" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="D" CommandArgument="D" Text="D" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="E" CommandArgument="E" Text="E" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="F" CommandArgument="F" Text="F" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="G" CommandArgument="G" Text="G" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="H" CommandArgument="H" Text="H" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="I" CommandArgument="I" Text="I" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="J" CommandArgument="J" Text="J" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="K" CommandArgument="K" Text="K" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="L" CommandArgument="L" Text="L" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="M" CommandArgument="M" Text="M" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="N" CommandArgument="N" Text="N" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="O" CommandArgument="O" Text="O" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="P" CommandArgument="P" Text="P" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="Q" CommandArgument="Q" Text="Q" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="R" CommandArgument="R" Text="R" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="S" CommandArgument="S" Text="S" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="T" CommandArgument="T" Text="T" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="U" CommandArgument="U" Text="U" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="V" CommandArgument="V" Text="V" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="W" CommandArgument="W" Text="W" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="X" CommandArgument="X" Text="X" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="Y" CommandArgument="Y" Text="Y" OnCommand="LetterGuessed" /> |
  <asp:LinkButton runat="server" id="Z" CommandArgument="Z" Text="Z" OnCommand="LetterGuessed" />
</center>
  <p>
  <center>
      <asp:label id="lblEndGameMessage" runat="server"
          Font-Size="18pt" Font-Weight="Bold" />
  </center>
</form>

</body>
</html>
