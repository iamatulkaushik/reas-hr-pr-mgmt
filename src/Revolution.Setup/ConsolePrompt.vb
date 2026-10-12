Friend NotInheritable Class ConsolePrompt
    Private Sub New()
    End Sub

    ''' <summary>Asks twice with hidden input. Returns Nothing when the two entries differ.</summary>
    Friend Shared Function ReadNewPassword() As String
        Dim first = ReadHidden("Password: ")
        Dim second = ReadHidden("Repeat password: ")
        Return If(String.Equals(first, second, StringComparison.Ordinal), first, Nothing)
    End Function

    ''' <summary>One hidden entry, for a password that is typed once (opening a backup).</summary>
    Friend Shared Function ReadSecret(prompt As String) As String
        Return ReadHidden(prompt)
    End Function

    Private Shared Function ReadHidden(prompt As String) As String
        Console.Write(prompt)
        Dim buffer As New Text.StringBuilder()
        Do
            Dim key = Console.ReadKey(True)
            If key.Key = ConsoleKey.Enter Then
                Exit Do
            ElseIf key.Key = ConsoleKey.Backspace Then
                If buffer.Length > 0 Then
                    buffer.Length -= 1
                End If
            ElseIf Not Char.IsControl(key.KeyChar) Then
                buffer.Append(key.KeyChar)
            End If
        Loop
        Console.WriteLine()
        Return buffer.ToString()
    End Function
End Class
