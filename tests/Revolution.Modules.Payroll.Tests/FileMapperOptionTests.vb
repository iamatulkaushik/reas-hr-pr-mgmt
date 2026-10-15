Imports Revolution.Modules.Payroll
Imports Xunit

Public Class FileMapperOptionTests
    Private Shared Function Worker() As PayrollFileEmployee
        Return New PayrollFileEmployee With {
            .EmployeeID = 1, .EmployeeCode = "E001", .Name = "Ravi Kumar", .PaidDays = 20D, .GrossEarnings = 25000D,
            .EpfWage = 25000D, .EsiWage = 25000D, .EpfEmployee = 3000D, .EpfEmployerEpf = 917D,
            .EpfEmployerEps = 2083D, .Edli = 75D, .EsiEmployee = 135D, .EsiEmployer = 585D, .NetPay = 20000D,
            .IsEpfMember = True, .Uan = "100123456789", .IsEsiMember = True, .EsiIpNumber = "2018517470"}
    End Function

    <Fact>
    Public Sub Ecr_EpsOnHigherWage_NotCapped()
        Dim w As PayrollFileEmployee = Worker()
        w.EpsOnHigherWage = True
        Dim m As EcrMember = EcrMapper.Map({w}, 15000D, New Date(2026, 9, 1)).Items(0)
        Assert.Equal(25000D, m.EpsWages)
        Assert.Equal(15000D, m.EdliWages)
        Assert.NotEmpty(EcrFileWriter.Build({m}, 15000D))
    End Sub

    <Fact>
    Public Sub Ecr_EpsDefault_CappedAtCeiling()
        Dim m As EcrMember = EcrMapper.Map({Worker()}, 15000D, New Date(2026, 9, 1)).Items(0)
        Assert.Equal(15000D, m.EpsWages)
        Assert.Equal(25000D, m.EpfWages)
    End Sub

    <Fact>
    Public Sub Validate_HighEpsWithoutOption_Fails()
        Dim m As New EcrMember("100123456789", "A", 25000D, 25000D, 25000D, 15000D, 3000D, 2083D, 917D, 0, 0D)
        Assert.NotEmpty(EcrValidator.Validate({m}, 15000D))
    End Sub

    <Fact>
    Public Sub Ecr_NcpDays_UseCalendarDaysOfMonth()
        Dim feb As EcrMember = EcrMapper.Map({Worker()}, 15000D, New Date(2027, 2, 1)).Items(0)
        Dim oct As EcrMember = EcrMapper.Map({Worker()}, 15000D, New Date(2026, 10, 1)).Items(0)
        Assert.Equal(8, feb.NcpDays)
        Assert.Equal(11, oct.NcpDays)
    End Sub

    <Fact>
    Public Sub EpfCalculator_PensionOnActualWage()
        Dim r As EpfResult = EpfCalculator.Calculate(20000D, StatutoryTestRates.Epf(), True, True)
        Assert.Equal(2400D, r.EmployeeShare)
        Assert.Equal(1666D, r.EmployerEps)
        Assert.Equal(734D, r.EmployerEpf)
        Assert.Equal(75D, r.Edli)
    End Sub

    <Fact>
    Public Sub EpfCalculator_PensionOnActualNeedsEpfOnActual()
        Assert.Throws(Of ArgumentException)(Function() EpfCalculator.Calculate(20000D, StatutoryTestRates.Epf(), False, True))
    End Sub

    <Fact>
    Public Sub Workbook_TwoSheets_SecondIsInstructionsOnly()
        Dim rows As IReadOnlyList(Of EsicRow) = EsicMapper.Map({Worker()}).Items
        Dim sheets As IReadOnlyList(Of ExportSheet) = EsicWorkbookBuilder.Build(rows, {2, 3, 4, 5, 6, 10}, 9)
        Assert.Equal(2, sheets.Count)
        Assert.Equal("Sheet1", sheets(0).Name)
        Assert.Equal("Instructions & Reason Codes", sheets(1).Name)
        Assert.Equal({"Reason", "Code", "Note"}, sheets(1).Table(0))
        Assert.Equal({"Left Service", "2"}, sheets(1).Table(3).Take(2).ToArray())
        Assert.DoesNotContain(sheets(1).Table, Function(r) r.Contains("2018517470"))
    End Sub

    <Fact>
    Public Sub Workbook_UploadSheetIsTemplateSheet()
        Dim rows As IReadOnlyList(Of EsicRow) = EsicMapper.Map({Worker()}).Items
        Dim sheets As IReadOnlyList(Of ExportSheet) = EsicWorkbookBuilder.Build(rows, {2, 3, 4, 5, 6, 10}, 9)
        Assert.Equal(2, sheets(0).Table.Count)
        Assert.Equal("2018517470", sheets(0).Table(1)(0))
        Assert.Equal(6, sheets(0).Table(0).Length)
    End Sub

    <Fact>
    Public Sub Instructions_HaveAllFourteenReasonCodes()
        Dim table As IReadOnlyList(Of String()) = EsicInstructions.Table()
        Dim codes As Integer = table.Where(Function(r) r.Length = 3 AndAlso r(1) <> "Code").Count()
        Assert.Equal(14, codes)
    End Sub

    <Fact>
    Public Sub DetailSheet_HasRowAndTotals_NotInWorkbook()
        Dim details As IReadOnlyList(Of EsicDetail) = EsicMapper.MapDetails({Worker()}).Items
        Dim sheet As ExportSheet = EsicDetailSheetBuilder.Build(details)
        Assert.Equal(3, sheet.Table.Count)
        Assert.Equal("E001", sheet.Table(1)(1))
        Assert.Equal("Total", sheet.Table(2)(3))
        Assert.Equal("720.00", sheet.Table(2)(9))
        Dim book As IReadOnlyList(Of ExportSheet) = EsicWorkbookBuilder.Build(EsicMapper.Map({Worker()}).Items, {2, 3, 4, 5, 6, 10}, 9)
        Assert.DoesNotContain(book, Function(x) x.Name = EsicDetailSheetBuilder.SheetName)
    End Sub

    <Fact>
    Public Sub BranchCode_FromFiveDigitsOrIfsc()
        Assert.Equal("08586", SbiBranchCode.Normalize("08586"))
        Assert.Equal("08586", SbiBranchCode.Normalize("SBIN0008586"))
    End Sub

    <Fact>
    Public Sub ToBatch_CompanyBranchCodeUsedDirectly()
        Dim payees As New List(Of BankPayee)()
        Dim batch As BankBatch = BankBatchMapper.ToBatch("01600123456", "08586", New Date(2026, 10, 31), "SAL1026", "SALARY", payees)
        Assert.Equal("08586", batch.DebitBranchCode)
    End Sub
End Class
