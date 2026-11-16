Imports Revolution.Core
Imports Xunit

Public Class MonthlyPackSheetBuilderTests
    Private Shared Function Pack() As MonthlyPack
        Dim epf As New MonthlyPackLine(ComplianceKind.Epf, "EPF", Nothing, Nothing, PackStatus.Ready, 125430.5D, 87, New Date(2026, 11, 15), Nothing, Nothing, Nothing,
                                       New List(Of PackAmount) From {New PackAmount("Employee share", 60000D), New PackAmount("Employer EPS", 40000.5D)})
        Dim lwf As New MonthlyPackLine(ComplianceKind.LabourWelfare, "Labour welfare fund", 6, "Haryana", PackStatus.Paid, Nothing, 87, Nothing, "Date not set", "CH-77", Nothing, Nothing)
        Return New MonthlyPack("Acme Works", New Date(2026, 10, 1), New List(Of MonthlyPackLine) From {epf, lwf}, String.Empty)
    End Function

    <Fact>
    Public Sub Build_WritesASummarySheetWithALinePerItem_AndABreakdownSheet()
        Dim sheets = MonthlyPackSheetBuilder.Build(Pack())
        Assert.Equal(2, sheets.Count)
        Assert.Equal(MonthlyPackSheetBuilder.SummarySheetName, sheets(0).Name)
        Assert.Equal(MonthlyPackSheetBuilder.BreakdownSheetName, sheets(1).Name)
        Assert.Equal(3, sheets(0).Table.Count)
        Assert.Equal(New String() {"EPF", "", "87", "125430.50", "2026-11-15", "Ready", ""}, sheets(0).Table(1))
        Assert.Equal(3, sheets(1).Table.Count)
        Assert.Equal(New String() {"EPF", "", "Employee share", "60000.00"}, sheets(1).Table(1))
    End Sub

    <Fact>
    Public Sub Build_ShowsTheDueNote_WhenThereIsNoDueDate_AndTheReferenceOfAPaidLine()
        Dim row = MonthlyPackSheetBuilder.Build(Pack())(0).Table(2)
        Assert.Equal("Haryana", row(1))
        Assert.Equal(String.Empty, row(3))
        Assert.Equal("Date not set", row(4))
        Assert.Equal("Paid", row(5))
        Assert.Equal("CH-77", row(6))
    End Sub

    <Fact>
    Public Sub Build_WithNoLines_StillHasTheHeaderRows()
        Dim empty As New MonthlyPack("Acme Works", New Date(2026, 10, 1), New List(Of MonthlyPackLine)(), "Payroll of the month is not approved.")
        Dim sheets = MonthlyPackSheetBuilder.Build(empty)
        Assert.Single(sheets(0).Table)
        Assert.Single(sheets(1).Table)
    End Sub

    <Fact>
    Public Sub ReadyCount_CountsReadyAndChangedLines()
        Dim lines As New List(Of MonthlyPackLine)()
        For Each status In New PackStatus() {PackStatus.NotReady, PackStatus.Ready, PackStatus.Changed, PackStatus.Paid}
            lines.Add(New MonthlyPackLine(ComplianceKind.Tds, "TDS", Nothing, Nothing, status, Nothing, 0, Nothing, Nothing, Nothing, Nothing, Nothing))
        Next
        Assert.Equal(2, New MonthlyPack("Acme", New Date(2026, 10, 1), lines, String.Empty).ReadyCount)
    End Sub

    <Theory>
    <InlineData(PackStatus.NotReady, "Not ready")>
    <InlineData(PackStatus.Generated, "Generated")>
    <InlineData(PackStatus.Changed, "Changed, make again")>
    Public Sub StatusText_UsesPlainWords(status As PackStatus, expected As String)
        Assert.Equal(expected, MonthlyPackSheetBuilder.StatusText(status))
    End Sub
End Class
