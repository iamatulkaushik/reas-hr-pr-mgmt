''' <summary>Checks bank details against the database rule: bank transfer needs an account and an IFSC.</summary>
Public NotInheritable Class BankDetailsValidator
    Private Sub New()
    End Sub

    ''' <param name="hasAccountOnFile">True when an account is already stored and this edit does not clear it.</param>
    Public Shared Function Validate(details As BankDetails, hasAccountOnFile As Boolean) As IReadOnlyList(Of String)
        If details Is Nothing Then
            Throw New ArgumentNullException(NameOf(details))
        End If
        Dim problems As New List(Of String)
        If Not [Enum].IsDefined(GetType(PaymentMode), details.PaymentMode) Then
            problems.Add("Choose how the employee is paid.")
        End If
        If details.BankId.HasValue AndAlso details.BankId.Value <= 0 Then
            problems.Add("Bank is not valid.")
        End If
        CheckIfsc(details, problems)
        CheckAccount(details, hasAccountOnFile, problems)
        Return problems
    End Function

    Private Shared Sub CheckIfsc(details As BankDetails, problems As List(Of String))
        Dim ifsc As String = If(details.Ifsc, String.Empty).Trim()
        If ifsc.Length > 0 AndAlso Not IfscValidator.IsValid(ifsc) Then
            problems.Add("IFSC format is wrong.")
        ElseIf ifsc.Length = 0 AndAlso details.PaymentMode = PaymentMode.BankTransfer Then
            problems.Add("Bank transfer needs an IFSC.")
        End If
    End Sub

    Private Shared Sub CheckAccount(details As BankDetails, hasAccountOnFile As Boolean, problems As List(Of String))
        Dim account As IdEdit = details.Account
        If account.Action = IdAction.Replace AndAlso Not BankAccountValidator.IsValid(account.Plain) Then
            problems.Add("Account number must be 9 to 18 digits.")
            Return
        End If
        Dim willHave As Boolean = account.Action = IdAction.Replace OrElse (account.Action = IdAction.Unchanged AndAlso hasAccountOnFile)
        If Not willHave AndAlso details.PaymentMode = PaymentMode.BankTransfer Then
            problems.Add("Bank transfer needs an account number.")
        End If
    End Sub
End Class
