Imports Revolution.Modules.Payroll
Imports Xunit

Public Class LoanRecoveryAllocatorTests
    Private Shared Function Open() As List(Of OpenInstallment)
        Return New List(Of OpenInstallment) From {
            New OpenInstallment(2, 1, 202610, 500D),
            New OpenInstallment(1, 3, 202610, 1000D),
            New OpenInstallment(1, 4, 202611, 1000D),
            New OpenInstallment(1, 5, 202612, 1000D)
        }
    End Function

    <Fact>
    Public Sub ScheduledEmi_IncludesDeferredAndCurrent_NotFuture()
        Assert.Equal(2500D, LoanRecoveryAllocator.ScheduledEmi(Open(), 202611))
    End Sub

    <Fact>
    Public Sub Allocate_OldestMonthThenLoanThenNumber()
        Dim a As IReadOnlyList(Of RecoveryAllocation) = LoanRecoveryAllocator.Allocate(1800D, Open(), 202611)
        Assert.Equal(3, a.Count)
        Assert.Equal(1, a(0).LoanID)
        Assert.Equal(3, a(0).InstallmentNo)
        Assert.Equal(1000D, a(0).Amount)
        Assert.Equal(2, a(1).LoanID)
        Assert.Equal(500D, a(1).Amount)
        Assert.Equal(1, a(2).LoanID)
        Assert.Equal(4, a(2).InstallmentNo)
        Assert.Equal(300D, a(2).Amount)
    End Sub

    <Fact>
    Public Sub Allocate_Total_EqualsAmountTaken()
        Dim a As IReadOnlyList(Of RecoveryAllocation) = LoanRecoveryAllocator.Allocate(2500D, Open(), 202611)
        Assert.Equal(2500D, a.Sum(Function(x) x.Amount))
    End Sub

    <Fact>
    Public Sub Allocate_Zero_NoRows()
        Assert.Equal(0, LoanRecoveryAllocator.Allocate(0D, Open(), 202611).Count)
    End Sub

    <Fact>
    Public Sub Allocate_MoreThanDue_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() LoanRecoveryAllocator.Allocate(2501D, Open(), 202611))
    End Sub

    <Fact>
    Public Sub NullList_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() LoanRecoveryAllocator.ScheduledEmi(Nothing, 202611))
    End Sub
End Class
