Imports System
Imports System.IO
Imports System.Collections
Imports System.Web
Imports System.Web.UI
Imports System.Web.SessionState
Imports System.Web.UI.WebControls


NameSpace NS_HM

Public Class HM
	Inherits Page

        Private sesObj As HttpSessionState
        Public hangmanImage As Image
        Public currentWord As Literal
        Public lblEndGameMessage As Label

        Public A As LinkButton
        Public B As LinkButton
        Public C As LinkButton
        Public D As LinkButton
        Public E As LinkButton
        Public F As LinkButton
        Public G As LinkButton
        Public H As LinkButton
        Public I As LinkButton
        Public J As LinkButton
        Public K As LinkButton
        Public L As LinkButton
        Public M As LinkButton
        Public N As LinkButton
        Public O As LinkButton
        Public P As LinkButton
        Public Q As LinkButton
        Public R As LinkButton
        Public S As LinkButton
        Public T As LinkButton
        Public U As LinkButton
        Public V As LinkButton
        Public W As LinkButton
        Public X As LinkButton
        Public Y As LinkButton
        Public Z As LinkButton

        Public page1 As page = Me

  	protected overrides Sub OnInit(e as EventArgs)
            InitializeComponent()
            MyBase.OnInit(e)
        End Sub

        Private Sub InitializeComponent()
            AddHandler page1.Load, AddressOf Me.Page_Load
        End Sub

        Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
            sesObj = MyBase.Session
            If Not Page.IsPostBack Then
                ResetGame()
            End If
        End Sub



        Public Sub ResetGame()
            'This is the first time the user is visiting the page,
            'use the defaults
            hangmanImage.ImageUrl = "/images/hang_0.gif"

            'Choose a random word from a text file
            sesObj = MyBase.Session

            sesObj("hangman_word") = GetRandomWord(Server.MapPath("/demos/hangmanWords.txt"))
            sesObj("wrong_guesses") = 0


            'Specify the current "guess", which is no letters guessed
            Dim i As Integer
            Dim initialGuess As String
            For i = 0 To sesObj("hangman_word").ToString().Length - 1
                initialGuess &= "*"
            Next i
            sesObj("current_word") = initialGuess

            'Put in blanks for the various letters
            DisplayCurrentWord()
        End Sub


        Public Sub DisplayCurrentWord()
            currentWord.Text = ""

            Dim i As Integer
            For i = 0 To sesObj("current_word").ToString().Length - 1
                If sesObj("current_word").ToString().Substring(i, 1) = "*" Then
                    currentWord.Text &= "_   "
                Else
                    currentWord.Text &= sesObj("current_word").ToString().Substring(i, 1).ToUpper() & _
                                       "   "
                End If
            Next i
        End Sub


        Public Sub LetterGuessed(ByVal sender As Object, ByVal e As CommandEventArgs)
            'First, make the letter selected disabled
            Dim clickedButton As LinkButton = FindControl(e.CommandArgument)
            clickedButton.Enabled = False
            '    clickedButton.ForeColor = Color.Red

            'Now, determine if the letter is in the word
            If sesObj("hangman_word").ToString().ToLower().IndexOf(e.CommandArgument.ToString().ToLower()) >= 0 Then
                'The letter was found
                Dim i As Integer
                Dim current As String = String.Empty
                For i = 0 To sesObj("hangman_word").ToString().Length - 1
                    If sesObj("hangman_word").ToString().Substring(i, 1).ToLower() = e.CommandArgument.ToString().ToLower() Then
                        current &= sesObj("hangman_word").ToString().Substring(i, 1)
                    Else
                        current &= sesObj("current_word").ToString().Substring(i, 1)
                    End If
                Next i

                sesObj("current_word") = current
                DisplayCurrentWord()

                'See if they have guessed the word correctly!
                If sesObj("hangman_word").ToString() = sesObj("current_word").ToString() Then
                    EndGame(True)
                End If
            Else
                'The letter was not found, increment the # of wrong guesses
                sesObj("wrong_guesses") = Convert.ToInt32(sesObj("wrong_guesses")) + 1

                'Update the hangman image
                hangmanImage.ImageUrl = "/images/hang_" & sesObj("wrong_guesses").ToString() & ".gif"

                If Convert.ToInt32(sesObj("wrong_guesses")) >= 6 Then
                    'Eep, the person has lost
                    EndGame(False)
                End If
            End If
        End Sub


        Public Sub EndGame(ByVal won As Boolean)
            If won Then
                lblEndGameMessage.Text = "Congratulations!  You won!"
            Else
                lblEndGameMessage.Text = "Sorry, you lost.  The correct word was: " & _
                sesObj("hangman_word").ToString().ToUpper()
                'lblEndGameMessage.ForeColor = Color.Red
            End If

            'lblEndGameMessage.Text &= "<p>Play Again!"
            lblEndGameMessage.Text &= "<p><a href=""hangman.aspx"">Play Again!</a>"
        End Sub


        Public Function GetRandomWord(ByVal filePath As String) As String
            'Open the file
            Dim objTextReader As TextReader = File.OpenText(filePath)

            'Read in all the lines into an ArrayList
            Dim words As ArrayList = New ArrayList
            Dim word As String = objTextReader.ReadLine()
            While Not word Is Nothing
                words.Add(word)
                word = objTextReader.ReadLine()
            End While

            'Close the Text file
            objTextReader.Close()

            'Now, randomly choose a word from the word ArrayList
            Dim rndNum As Random = New Random
            Dim iLine As Integer = rndNum.Next(words.Count)
            Dim selectedWord As String = words(iLine)

            Return selectedWord
        End Function

End Class

End NameSpace