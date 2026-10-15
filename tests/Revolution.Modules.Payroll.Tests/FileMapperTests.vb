Imports Revolution.Modules.Payroll
Imports Xunit

Public Class FileMapperTests
    Private Shared Function Worker(Optional id As Integer = 1) As PayrollFileEmployee
        Return New PayrollFileEmployee With {
            .EmployeeID = id, .Name = "Ravi Kumar", .PaidDays = 25.5D, .GrossEarnings = 18000.4D,
            .EpfWage = 18000.4D, .EsiWage = 18000.4D, .EpfEmployee = 1800D, .EpfEmployerEpf = 550D,
            .EpfEmployerEps = 1250D, .Edli = 75D, .NetPay = 15000D,
            .IsEpfMember = True, .Uan = "100123456789", .IsEsiMember = True, .EsiIpNumber = "2018517470",
            .PaysByBank = True, .Ifsc = "SBIN0008586", .AccountNumber = "01004567891"}
    End Function

    <Fact>
    Public Sub Ecr_CapsEpsAndEdliAtCeiling()
        Dim m As EcrMember = EcrMapper.Map({Worker()}, 15000D, New Date(2026, 9, 1)).Items(0)
        Assert.Equal(18000D, m.GrossWages)
        Assert.Equal(18000D, m.EpfWages)
        Assert.Equal(15000D, m.EpsWages)
        Assert.Equal(15000D, m.EdliWages)
        Assert.Equal(1250D, m.EpsContribution)
        Assert.Equal(550D, m.EmployerDifference)
    End Sub

    <Fact>
    Public Sub Ecr_NcpDays_BaseLessPaidDaysRoundedUp()
        Assert.Equal(4, EcrMapper.Map({Worker()}, 15000D, New Date(2026, 9, 1)).Items(0).NcpDays)
    End Sub

    <Fact>
    Public Sub Ecr_NonMemberLeftOut()
        Dim w As PayrollFileEmployee = Worker()
        w.IsEpfMember = False
        Assert.Empty(EcrMapper.Map({w}, 15000D, New Date(2026, 9, 1)).Items)
    End Sub

    <Fact>
    Public Sub Ecr_MissingUan_ReportedAndLeftOut()
        Dim w As PayrollFileEmployee = Worker(7)
        w.Uan = ""
        Dim r As MapResult(Of EcrMember) = EcrMapper.Map({w}, 15000D, New Date(2026, 9, 1))
        Assert.Empty(r.Items)
        Assert.Contains("Employee 7", r.Problems(0), StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub Ecr_NoPensionShare_EpsWagesZero()
        Dim w As PayrollFileEmployee = Worker()
        w.EpfEmployerEps = 0D
        Assert.Equal(0D, EcrMapper.Map({w}, 15000D, New Date(2026, 9, 1)).Items(0).EpsWages)
    End Sub

    <Fact>
    Public Sub Esic_DaysRoundUp_WagesWhole()
        Dim row As EsicRow = EsicMapper.Map({Worker()}).Items(0)
        Assert.Equal(26, row.DaysPaid)
        Assert.Equal(18000D, row.TotalWages)
    End Sub

    <Fact>
    Public Sub Esic_MissingIp_ReportedAndLeftOut()
        Dim w As PayrollFileEmployee = Worker(3)
        w.EsiIpNumber = ""
        Dim r As MapResult(Of EsicRow) = EsicMapper.Map({w})
        Assert.Empty(r.Items)
        Assert.Single(r.Problems)
    End Sub

    <Fact>
    Public Sub Bank_SbiPayee_SameBankWithBranchCode()
        Dim p As BankPayee = BankBatchMapper.Map({Worker()}, "SBIN", PaymentMode.Neft, "SAL1026").Items(0)
        Assert.True(p.IsSameBank)
        Assert.Equal("08586", p.BranchOrIfsc)
        Assert.Equal("SAL10260001", p.Reference)
        Assert.Equal("RAVI KUMAR", p.Name)
    End Sub

    <Fact>
    Public Sub Bank_OtherBankKeepsIfsc()
        Dim w As PayrollFileEmployee = Worker()
        w.Ifsc = "BKID0008605"
        Dim p As BankPayee = BankBatchMapper.Map({w}, "SBIN", PaymentMode.Neft, "SAL1026").Items(0)
        Assert.False(p.IsSameBank)
        Assert.Equal("BKID0008605", p.BranchOrIfsc)
    End Sub

    <Fact>
    Public Sub Bank_CashOrZeroNet_LeftOut()
        Dim cash As PayrollFileEmployee = Worker(1)
        cash.PaysByBank = False
        Dim zero As PayrollFileEmployee = Worker(2)
        zero.NetPay = 0D
        Assert.Empty(BankBatchMapper.Map({cash, zero}, "SBIN", PaymentMode.Neft, "SAL1026").Items)
    End Sub

    <Fact>
    Public Sub Bank_MissingAccount_ReportedAndLeftOut()
        Dim w As PayrollFileEmployee = Worker(5)
        w.AccountNumber = ""
        Dim r As MapResult(Of BankPayee) = BankBatchMapper.Map({w}, "SBIN", PaymentMode.Neft, "SAL1026")
        Assert.Empty(r.Items)
        Assert.Single(r.Problems)
    End Sub

    <Fact>
    Public Sub EndToEnd_MappedRowsBuildFiles()
        Dim staff As PayrollFileEmployee() = {Worker(1), Worker(2)}
        staff(1).Uan = "100123456790"
        staff(1).EsiIpNumber = "2018517471"
        staff(1).AccountNumber = "01004567892"
        Assert.NotEmpty(EcrFileWriter.Build(EcrMapper.Map(staff, 15000D, New Date(2026, 9, 1)).Items, 15000D))
        Assert.Equal(3, EsicSheetBuilder.Build(EsicMapper.Map(staff).Items, {2, 3, 4, 5, 6, 10}, 10).Count)
        Dim payees As IReadOnlyList(Of BankPayee) = BankBatchMapper.Map(staff, "SBIN", PaymentMode.Neft, "SAL1026").Items
        Dim batch As New BankBatch("01600123456", "08586", New Date(2026, 10, 31), "SAL1026", "SALARY OCT 2026", payees)
        Assert.NotEmpty(New SbiFileWriter().Build(batch).TransactionText)
    End Sub
End Class
