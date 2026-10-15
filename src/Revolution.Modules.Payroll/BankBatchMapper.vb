Imports System.Globalization

''' <summary>
''' Maps run lines to one bank batch. Only employees paid by bank with net pay above zero.
''' Same bank when the IFSC starts with the bank prefix. SBI branch code = last 5 characters of the IFSC (confirm with the bank).
''' Payee reference = batch reference + 4 digit serial, so the batch reference can be at most 11 characters.
''' </summary>
Public NotInheritable Class BankBatchMapper
    Private Sub New()
    End Sub

    Public Shared Function Map(employees As IEnumerable(Of PayrollFileEmployee), ifscPrefix As String, mode As PaymentMode,
                               batchReference As String) As MapResult(Of BankPayee)
        If employees Is Nothing Then
            Throw New ArgumentNullException(NameOf(employees))
        End If
        Dim items As New List(Of BankPayee)()
        Dim problems As New List(Of String)()
        For Each e As PayrollFileEmployee In employees.Where(Function(x) x.PaysByBank AndAlso x.NetPay > 0D)
            If String.IsNullOrWhiteSpace(e.AccountNumber) OrElse String.IsNullOrWhiteSpace(e.Ifsc) OrElse e.Ifsc.Trim().Length <> 11 Then
                problems.Add($"Employee {e.EmployeeID}: bank account or IFSC is missing. Left out of bank file.")
            Else
                items.Add(ToPayee(e, ifscPrefix, mode, batchReference & (items.Count + 1).ToString("D4", CultureInfo.InvariantCulture)))
            End If
        Next
        Return New MapResult(Of BankPayee)(items, problems)
    End Function

    ''' <summary>Company bank value may be the 5 digit branch code or a full IFSC.</summary>
    Public Shared Function ToBatch(debitAccount As String, companyBranchOrIfsc As String, paymentDate As Date, batchReference As String,
                                   description As String, payees As IReadOnlyList(Of BankPayee)) As BankBatch
        Return New BankBatch(debitAccount, SbiBranchCode.Normalize(companyBranchOrIfsc), paymentDate, batchReference, description, payees)
    End Function

    Private Shared Function ToPayee(e As PayrollFileEmployee, ifscPrefix As String, mode As PaymentMode, reference As String) As BankPayee
        Dim ifsc As String = e.Ifsc.Trim().ToUpperInvariant()
        Dim sameBank As Boolean = ifsc.StartsWith(ifscPrefix, StringComparison.Ordinal)
        Dim branchOrIfsc As String = If(sameBank, SbiBranchCode.Normalize(ifsc), ifsc)
        Return New BankPayee(EcrNameCleaner.Clean(e.Name), e.AccountNumber.Trim(), branchOrIfsc, sameBank, e.NetPay, reference, mode)
    End Function
End Class
