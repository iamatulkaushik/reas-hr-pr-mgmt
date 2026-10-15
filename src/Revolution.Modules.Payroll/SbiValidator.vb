Imports System.Text.RegularExpressions

''' <summary>Checks a bank batch against the SBI composite beneficiary and transaction file limits.</summary>
Public NotInheritable Class SbiValidator
    Private Shared ReadOnly Ifsc As New Regex("^[A-Z]{4}0[A-Z0-9]{6}$", RegexOptions.CultureInvariant)

    Private Sub New()
    End Sub

    Public Shared Function Validate(batch As BankBatch, options As SbiFileOptions) As IReadOnlyList(Of String)
        If batch Is Nothing Then
            Throw New ArgumentNullException(NameOf(batch))
        End If
        Dim errors As New List(Of String)()
        CheckBatch(batch, options, errors)
        Dim seen As New HashSet(Of String)(StringComparer.Ordinal)
        Dim number As Integer = 0
        For Each p As BankPayee In If(batch.Payees, New List(Of BankPayee)())
            number += 1
            CheckPayee(p, number, options, errors)
            If Not seen.Add(If(p.Reference, String.Empty)) Then
                errors.Add($"Payee {number}: reference is repeated.")
            End If
        Next
        Return errors
    End Function

    Private Shared Sub CheckBatch(batch As BankBatch, options As SbiFileOptions, errors As List(Of String))
        If Not IsDigits(batch.DebitAccount, 17) Then
            errors.Add("Debit account must be digits, up to 17.")
        End If
        If batch.DebitBranchCode Is Nothing OrElse batch.DebitBranchCode.Length <> 5 OrElse Not IsDigits(batch.DebitBranchCode, 5) Then
            errors.Add("Debit branch code must be 5 digits.")
        End If
        If String.IsNullOrEmpty(batch.BatchReference) OrElse batch.BatchReference.Length > 15 Then
            errors.Add("Batch reference must be 1 to 15 characters.")
        End If
        If batch.Description Is Nothing OrElse batch.Description.Length > 240 Then
            errors.Add("Description must be up to 240 characters.")
        End If
        If batch.Payees Is Nothing OrElse batch.Payees.Count = 0 Then
            errors.Add("Batch has no payees.")
        End If
        CheckDelimiter(batch.Description, "Description", options, errors)
    End Sub

    Private Shared Sub CheckPayee(p As BankPayee, number As Integer, options As SbiFileOptions, errors As List(Of String))
        If String.IsNullOrWhiteSpace(p.Name) OrElse p.Name.Length > 35 Then
            errors.Add($"Payee {number}: name must be 1 to 35 characters.")
        End If
        If p.Amount <= 0D OrElse p.Amount <> Decimal.Round(p.Amount, 2) Then
            errors.Add($"Payee {number}: amount must be above zero with at most 2 decimals.")
        End If
        If String.IsNullOrEmpty(p.Reference) OrElse p.Reference.Length > 15 Then
            errors.Add($"Payee {number}: reference must be 1 to 15 characters.")
        End If
        CheckAccount(p, number, errors)
        CheckDelimiter(p.Name, $"Payee {number} name", options, errors)
        CheckDelimiter(p.Reference, $"Payee {number} reference", options, errors)
    End Sub

    Private Shared Sub CheckAccount(p As BankPayee, number As Integer, errors As List(Of String))
        If String.IsNullOrEmpty(p.AccountNumber) OrElse p.AccountNumber.Length > 17 Then
            errors.Add($"Payee {number}: account must be 1 to 17 characters.")
        End If
        If p.IsSameBank Then
            If Not IsDigits(p.AccountNumber, 17) Then
                errors.Add($"Payee {number}: same bank account must be digits.")
            End If
            If Not IsDigits(p.BranchOrIfsc, 5) OrElse p.BranchOrIfsc.Length <> 5 Then
                errors.Add($"Payee {number}: same bank branch code must be 5 digits.")
            End If
        ElseIf p.BranchOrIfsc Is Nothing OrElse Not Ifsc.IsMatch(p.BranchOrIfsc) Then
            errors.Add($"Payee {number}: IFSC code is not valid.")
        End If
    End Sub

    Private Shared Sub CheckDelimiter(text As String, label As String, options As SbiFileOptions, errors As List(Of String))
        If text Is Nothing Then
            Return
        End If
        If text.Contains(options.BeneficiaryDelimiter) OrElse text.Contains(options.TransactionDelimiter) Then
            errors.Add($"{label} contains a file delimiter.")
        End If
    End Sub

    Private Shared Function IsDigits(value As String, maxLength As Integer) As Boolean
        Return Not String.IsNullOrEmpty(value) AndAlso value.Length <= maxLength AndAlso value.All(AddressOf Char.IsDigit)
    End Function
End Class
