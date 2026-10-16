''' <summary>The command line: a command word, then positional values and "--name value" options.</summary>
Public NotInheritable Class Arguments
    Private ReadOnly _options As Dictionary(Of String, String)

    Private Sub New(command As String, positional As List(Of String), options As Dictionary(Of String, String))
        Me.Command = command
        Me.Positional = positional
        _options = options
    End Sub

    Public ReadOnly Property Command As String
    Public ReadOnly Property Positional As IReadOnlyList(Of String)

    Public Shared Function Parse(args As String()) As Arguments
        Dim words As New List(Of String)()
        Dim found As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Dim index = 1
        Do While index < args.Length
            Dim token = args(index)
            If token.StartsWith("--", StringComparison.Ordinal) Then
                If index + 1 >= args.Length OrElse args(index + 1).StartsWith("--", StringComparison.Ordinal) Then
                    Throw New ArgumentException($"Option {token} needs a value.")
                End If
                found(token.Substring(2)) = args(index + 1)
                index += 2
            Else
                words.Add(token)
                index += 1
            End If
        Loop
        Return New Arguments(If(args.Length > 0, args(0).ToLowerInvariant(), String.Empty), words, found)
    End Function

    ''' <summary>The option's value, or Nothing when it was not given.</summary>
    Public Function Find(name As String) As String
        Dim value As String = Nothing
        Return If(_options.TryGetValue(name, value), value, Nothing)
    End Function

    ''' <summary>The option's value. Throws ArgumentException when it was not given.</summary>
    Public Function Need(name As String) As String
        Dim value = Find(name)
        If value Is Nothing Then
            Throw New ArgumentException($"Missing --{name}.")
        End If
        Return value
    End Function
End Class
