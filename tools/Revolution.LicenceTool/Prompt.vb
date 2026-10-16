Imports System.Text

''' <summary>Reads a password from the console without showing it.</summary>
Public NotInheritable Class Prompt
    Private Sub New()
    End Sub

    Public Shared Function ReadSecret(message As String) As String
        Console.Write(message)
        Dim typed As New StringBuilder()
        Do
            Dim pressed = Console.ReadKey(True)
            If pressed.Key = ConsoleKey.Enter Then
                Console.WriteLine()
                Return typed.ToString()
            End If
            If pressed.Key = ConsoleKey.Backspace Then
                If typed.Length > 0 Then
                    typed.Length -= 1
                End If
            ElseIf pressed.KeyChar <> ChrW(0) Then
                typed.Append(pressed.KeyChar)
            End If
        Loop
    End Function
End Class
