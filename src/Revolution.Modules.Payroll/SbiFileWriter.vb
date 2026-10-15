Imports System.Globalization
Imports System.Text

''' <summary>
''' SBI files: composite beneficiary file (9 fields) and transaction file (8 fields).
''' Transaction file: one debit line, then one credit line per payee.
''' </summary>
Public NotInheritable Class SbiFileWriter
    Implements IBankFileWriter

    Private ReadOnly _options As SbiFileOptions

    Public Sub New(Optional options As SbiFileOptions = Nothing)
        _options = If(options, New SbiFileOptions())
    End Sub

    Public ReadOnly Property BankName As String Implements IBankFileWriter.BankName
        Get
            Return "State Bank of India"
        End Get
    End Property

    Public ReadOnly Property IfscPrefix As String Implements IBankFileWriter.IfscPrefix
        Get
            Return "SBIN"
        End Get
    End Property

    Public Function Build(batch As BankBatch) As BankFiles Implements IBankFileWriter.Build
        Dim problems As IReadOnlyList(Of String) = SbiValidator.Validate(batch, _options)
        If problems.Count > 0 Then
            Throw New InvalidOperationException("SBI batch is not valid: " & String.Join(" ", problems))
        End If
        Return New BankFiles(BeneficiaryFile(batch), TransactionFile(batch))
    End Function

    Public Function BeneficiaryLine(p As BankPayee) As String
        Dim fields As String() = {
            If(p.IsSameBank, "S", "O"), "A", EcrNameCleaner.Clean(p.Name), p.AccountNumber,
            p.BeneficiaryCode, p.BranchOrIfsc, String.Empty, String.Empty, String.Empty}
        Return String.Join(_options.BeneficiaryDelimiter, fields)
    End Function

    Public Function CreditLine(batch As BankBatch, p As BankPayee) As String
        Dim fields As String() = {
            p.AccountNumber, p.BranchOrIfsc, DateText(batch.PaymentDate), String.Empty,
            Money(p.Amount), p.Reference, batch.Description, ModeText(p)}
        Return String.Join(_options.TransactionDelimiter, fields)
    End Function

    Public Function DebitLine(batch As BankBatch) As String
        Dim total As Decimal = batch.Payees.Sum(Function(p) p.Amount)
        Dim fields As String() = {
            batch.DebitAccount, batch.DebitBranchCode, DateText(batch.PaymentDate), Money(total),
            String.Empty, batch.BatchReference, batch.Description, String.Empty}
        Return String.Join(_options.TransactionDelimiter, fields)
    End Function

    Private Function BeneficiaryFile(batch As BankBatch) As String
        Dim text As New StringBuilder()
        For Each p As BankPayee In batch.Payees
            text.Append(BeneficiaryLine(p)).Append(vbCrLf)
        Next
        Return text.ToString()
    End Function

    Private Function TransactionFile(batch As BankBatch) As String
        Dim text As New StringBuilder()
        text.Append(DebitLine(batch)).Append(vbCrLf)
        For Each p As BankPayee In batch.Payees
            text.Append(CreditLine(batch, p)).Append(vbCrLf)
        Next
        Return text.ToString()
    End Function

    Private Shared Function ModeText(p As BankPayee) As String
        If p.IsSameBank Then
            Return String.Empty
        End If
        Return If(p.Mode = PaymentMode.Rtgs, "RTGS", "NEFT")
    End Function

    Private Shared Function DateText(value As Date) As String
        Return value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
    End Function

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("0.00", CultureInfo.InvariantCulture)
    End Function
End Class
