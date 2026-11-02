Imports Revolution.Modules.Arrears
Imports Xunit

Public Class ArrearsReportTests
    Private Shared Function Row(employee As Integer, month As Integer, gross As Decimal, epf As Decimal, esi As Decimal, employer As Decimal) As ArrearsReportRow
        Return New ArrearsReportRow(employee, employee, "E" & employee, "Name " & employee, New Date(2026, month, 1), gross, epf, esi, employer)
    End Function

    <Fact>
    Public Sub Totals_AddTheRows()
        Dim report As New ArrearsReport(New Date(2026, 5, 1), New Date(2026, 6, 1), New List(Of ArrearsReportRow) From {
            Row(1, 5, 2000D, 240D, 15D, 305D), Row(1, 6, 2000D, 240D, 15D, 305D), Row(2, 5, 1000D, 120D, 0D, 100D)})
        Assert.Equal(2, report.EmployeeCount)
        Assert.Equal(5000D, report.GrossTotal)
        Assert.Equal(630D, report.EmployeeDeductionTotal)
        Assert.Equal(4370D, report.NetBeforeTax)
        Assert.Equal(710D, report.EmployerTotal)
    End Sub

    <Fact>
    Public Sub NoRows_AllZero()
        Dim report As New ArrearsReport(New Date(2026, 5, 1), New Date(2026, 6, 1), New List(Of ArrearsReportRow)())
        Assert.Equal(0, report.EmployeeCount)
        Assert.Equal(0D, report.NetBeforeTax)
    End Sub

    <Fact>
    Public Sub Overpayment_ReducesTheTotals()
        Dim report As New ArrearsReport(New Date(2026, 5, 1), New Date(2026, 6, 1), New List(Of ArrearsReportRow) From {
            Row(1, 5, 2000D, 240D, 0D, 0D), Row(1, 6, -500D, -60D, 0D, 0D)})
        Assert.Equal(1500D, report.GrossTotal)
        Assert.Equal(180D, report.EmployeeDeductionTotal)
    End Sub

    <Fact>
    Public Sub MonthsJson_HasOneObjectPerMonth_WithIsoDates()
        Dim months As New List(Of ArrearsMonthRecord) From {
            New ArrearsMonthRecord(New Date(2026, 5, 1), 10000D, 12000D, 2000D, 2000D, 2000D, 2000D, 240D, 73D, 167D, 10D, 10D, 15D, 65D)}
        Dim json As String = ArrearsMonthsJson.Build(months)
        Assert.Contains("""wageMonth"":""2026-05-01""", json, StringComparison.Ordinal)
        Assert.Contains("""grossDifference"":2000", json, StringComparison.Ordinal)
        Assert.Contains("""esiEmployer"":65", json, StringComparison.Ordinal)
        Assert.StartsWith("[{", json, StringComparison.Ordinal)
    End Sub
End Class
