Imports Revolution.Modules.Payroll
Imports Xunit

Public Class SbiFileWriterTests
    Private Shared Function Batch(ParamArray payees As BankPayee()) As BankBatch
        Return New BankBatch("01600123456", "08586", New Date(2026, 10, 31), "SAL102026", "SALARY OCT 2026", payees)
    End Function

    Private Shared Function SamePayee(Optional reference As String = "R1") As BankPayee
        Return New BankPayee("Ravi Kumar", "01004567891", "08586", True, 18000.5D, reference, PaymentMode.Neft)
    End Function

    Private Shared Function OtherPayee(Optional reference As String = "R2") As BankPayee
        Return New BankPayee("Sita Devi", "123456789012", "BKID0008605", False, 20000D, reference, PaymentMode.Neft)
    End Function

    <Fact>
    Public Sub Beneficiary_SameBank_NineFields()
        Dim line As String = New SbiFileWriter().BeneficiaryLine(SamePayee())
        Assert.Equal("S|A|RAVI KUMAR|01004567891||08586|||", line)
    End Sub

    <Fact>
    Public Sub Beneficiary_OtherBank_HasIfsc()
        Dim line As String = New SbiFileWriter().BeneficiaryLine(OtherPayee())
        Assert.Equal("O|A|SITA DEVI|123456789012||BKID0008605|||", line)
    End Sub

    <Fact>
    Public Sub Transaction_DebitLineHasTotal()
        Dim writer As New SbiFileWriter()
        Dim b As BankBatch = Batch(SamePayee(), OtherPayee())
        Assert.Equal("01600123456|08586|31/10/2026|38000.50||SAL102026|SALARY OCT 2026|", writer.DebitLine(b))
    End Sub

    <Fact>
    Public Sub Transaction_CreditLines_ModeOnlyForOtherBank()
        Dim writer As New SbiFileWriter()
        Dim b As BankBatch = Batch(SamePayee(), OtherPayee())
        Assert.Equal("01004567891|08586|31/10/2026||18000.50|R1|SALARY OCT 2026|", writer.CreditLine(b, SamePayee()))
        Assert.Equal("123456789012|BKID0008605|31/10/2026||20000.00|R2|SALARY OCT 2026|NEFT", writer.CreditLine(b, OtherPayee()))
    End Sub

    <Fact>
    Public Sub Build_TransactionFile_DebitThenCredits()
        Dim files As BankFiles = New SbiFileWriter().Build(Batch(SamePayee(), OtherPayee()))
        Dim lines As String() = files.TransactionText.Split({vbCrLf}, StringSplitOptions.RemoveEmptyEntries)
        Assert.Equal(3, lines.Length)
        Assert.Equal(2, files.BeneficiaryText.Split({vbCrLf}, StringSplitOptions.RemoveEmptyEntries).Length)
    End Sub

    <Fact>
    Public Sub Build_CustomDelimiter_Used()
        Dim writer As New SbiFileWriter(New SbiFileOptions("#", "#"))
        Assert.StartsWith("S#A#", writer.BeneficiaryLine(SamePayee()), StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub Validate_RepeatedReference_Fails()
        Dim b As BankBatch = Batch(SamePayee("R1"), OtherPayee("R1"))
        Assert.NotEmpty(SbiValidator.Validate(b, New SbiFileOptions()))
    End Sub

    <Fact>
    Public Sub Validate_BadIfsc_Fails()
        Dim p As New BankPayee("A", "123", "BAD", False, 10D, "R1", PaymentMode.Neft)
        Assert.NotEmpty(SbiValidator.Validate(Batch(p), New SbiFileOptions()))
    End Sub

    <Fact>
    Public Sub Validate_LongName_Fails()
        Dim p As New BankPayee(New String("A"c, 36), "01004567891", "08586", True, 10D, "R1", PaymentMode.Neft)
        Assert.NotEmpty(SbiValidator.Validate(Batch(p), New SbiFileOptions()))
    End Sub

    <Fact>
    Public Sub Validate_AmountWithThreeDecimals_Fails()
        Dim p As New BankPayee("A", "01004567891", "08586", True, 10.125D, "R1", PaymentMode.Neft)
        Assert.NotEmpty(SbiValidator.Validate(Batch(p), New SbiFileOptions()))
    End Sub

    <Fact>
    Public Sub Validate_DelimiterInName_Fails()
        Dim p As New BankPayee("A|B", "01004567891", "08586", True, 10D, "R1", PaymentMode.Neft)
        Assert.NotEmpty(SbiValidator.Validate(Batch(p), New SbiFileOptions()))
    End Sub

    <Fact>
    Public Sub Build_InvalidBatch_Throws()
        Dim p As New BankPayee("A", "123", "BAD", False, 10D, "R1", PaymentMode.Neft)
        Assert.Throws(Of InvalidOperationException)(Function() New SbiFileWriter().Build(Batch(p)))
    End Sub
End Class
