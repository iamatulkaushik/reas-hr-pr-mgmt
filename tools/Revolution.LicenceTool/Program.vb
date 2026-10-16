''' <summary>
''' Revolution licence tool: the Developer's offline tool for making licences (ADR 0018). Not shipped to clients.
'''   keygen  --key-id ID --out FILE                 make the signing key pair (once)
'''   issue   --key FILE --out FILE --associate NAME --installation GUID --plan Copper|Silver|Gold|Platinum --users N
'''           [--companies N] [--modules a,b,c|all] [--from yyyy-MM-dd] [--to yyyy-MM-dd] [--issued-by NAME] [--note TEXT]
'''   inspect FILE [--key FILE] [--installation GUID]  show a licence, and check it when the key is given
''' </summary>
Public Module Program
    Public Function Main(args As String()) As Integer
        Try
            Dim parsed = Arguments.Parse(args)
            Select Case parsed.Command
                Case "keygen"
                    Return LicenceCommands.KeyGen(parsed)
                Case "issue"
                    Return LicenceCommands.Issue(parsed)
                Case "inspect"
                    Return LicenceCommands.Inspect(parsed)
                Case Else
                    Console.Error.WriteLine(Usage())
                    Return 2
            End Select
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is FormatException OrElse TypeOf ex Is IO.IOException _
            OrElse TypeOf ex Is System.Security.Cryptography.CryptographicException
            Console.Error.WriteLine("Failed: " & ex.Message)
            Return 1
        End Try
    End Function

    Private Function Usage() As String
        Return "RevolutionLicenceTool <command>" & vbLf &
               "  keygen  --key-id ID --out FILE" & vbLf &
               "  issue   --key FILE --out FILE --associate NAME --installation GUID --plan Copper|Silver|Gold|Platinum --users N" & vbLf &
               "          [--companies N] [--modules a,b,c|all] [--from yyyy-MM-dd] [--to yyyy-MM-dd] [--issued-by NAME] [--note TEXT]" & vbLf &
               "  inspect FILE [--key FILE] [--installation GUID]"
    End Function
End Module
