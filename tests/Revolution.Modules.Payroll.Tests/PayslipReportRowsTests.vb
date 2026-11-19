Imports Revolution.Modules.Payroll
Imports Xunit

Public Class PayslipReportRowsTests
    Private Shared Function Slip(employeeId As Integer, earnings As IEnumerable(Of PayslipEarning), overtime As Decimal, Optional showTds As Boolean = False,
                                 Optional status As PayrollRunStatus = PayrollRunStatus.Approved) As Payslip
        Dim row As New PayslipRow With {.RunId = 5, .RunStatus = status, .EmployeeId = employeeId, .EmpCode = "E" & employeeId.ToString(),
                                        .OvertimeAmount = overtime, .NetPay = 14270D, .Earnings = earnings.ToList()}
        Return New Payslip(row, New PayslipIds("100123456789", "1234567890", "30112233445", False), "Rupees Fourteen Thousand Two Hundred Seventy Only", status < PayrollRunStatus.Approved, showTds)
    End Function

    Private Shared Function Earning(name As String, order As Integer, amount As Decimal) As PayslipEarning
        Return New PayslipEarning(name.ToUpperInvariant(), name, order, amount)
    End Function

    <Fact>
    Public Sub OneRowPerEarningLine_InSortOrder()
        Dim rows = PayslipReportRows.Build({Slip(1, {Earning("HRA", 20, 4800D), Earning("Basic", 10, 16000D)}, 0D)})
        Assert.Equal(2, rows.Count)
        Assert.Equal("Basic", rows(0)("ComponentName"))
        Assert.Equal("HRA", rows(1)("ComponentName"))
        Assert.Equal(16000D, rows(0)("ComponentAmount"))
    End Sub

    <Fact>
    Public Sub Overtime_IsTheLastLine_OnlyWhenAboveZero()
        Dim withOt = PayslipReportRows.Build({Slip(1, {Earning("Basic", 10, 16000D)}, 2000D)})
        Assert.Equal(2, withOt.Count)
        Assert.Equal(PayslipReportRows.OvertimeLabel, withOt(1)("ComponentName"))
        Assert.Equal(2000D, withOt(1)("ComponentAmount"))
        Assert.Single(PayslipReportRows.Build({Slip(1, {Earning("Basic", 10, 16000D)}, 0D)}))
    End Sub

    <Fact>
    Public Sub NoEarningLines_StillGivesOneRow()
        Dim rows = PayslipReportRows.Build({Slip(1, New List(Of PayslipEarning)(), 0D)})
        Assert.Single(rows)
        Assert.Equal(PayslipReportRows.NoEarningsLabel, rows(0)("ComponentName"))
        Assert.Equal(0D, rows(0)("ComponentAmount"))
    End Sub

    <Fact>
    Public Sub EmployeeColumns_RepeatOnEveryRow_AndIdsComeFromThePayslip()
        Dim rows = PayslipReportRows.Build({Slip(7, {Earning("Basic", 10, 1D), Earning("HRA", 20, 2D)}, 0D)})
        For Each r In rows
            Assert.Equal(7D, r("EmployeeID"))
            Assert.Equal("100123456789", r("Uan"))
            Assert.Equal("1234567890", r("EsiIpNumber"))
            Assert.Equal("30112233445", r("BankAccountNumber"))
            Assert.Equal(14270D, r("NetPay"))
        Next
    End Sub

    <Fact>
    Public Sub NoEncryptedOrLast4Columns_AreHandedToTheLayout()
        Dim keys = PayslipReportRows.Build({Slip(1, {Earning("Basic", 10, 1D)}, 0D)})(0).Keys
        Assert.DoesNotContain(keys, Function(k) k.EndsWith("Enc", StringComparison.Ordinal) OrElse k.EndsWith("Last4", StringComparison.Ordinal) OrElse k.EndsWith("KeyVersion", StringComparison.Ordinal))
    End Sub

    <Fact>
    Public Sub RunStatus_IsANumber_SoTheLayoutCanTestForDraft()
        Dim rows = PayslipReportRows.Build({Slip(1, {Earning("Basic", 10, 1D)}, 0D, False, PayrollRunStatus.Draft)})
        Assert.True(CDec(rows(0)("RunStatusID")) < CDec(CInt(PayrollRunStatus.Approved)))
    End Sub

    <Fact>
    Public Sub ShowTds_FollowsThePayslip()
        Assert.True(PayslipReportRows.ShowTds({Slip(1, {Earning("Basic", 10, 1D)}, 0D, True)}))
        Assert.False(PayslipReportRows.ShowTds({Slip(1, {Earning("Basic", 10, 1D)}, 0D, False)}))
        Assert.False(PayslipReportRows.ShowTds(New List(Of Payslip)()))
    End Sub

    <Fact>
    Public Sub NullList_Throws()
        Assert.Throws(Of ArgumentNullException)(Sub() PayslipReportRows.Build(Nothing))
    End Sub
End Class
