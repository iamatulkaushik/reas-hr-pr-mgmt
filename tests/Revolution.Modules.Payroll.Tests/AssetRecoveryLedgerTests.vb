Imports Revolution.Modules.Payroll
Imports Xunit

Friend NotInheritable Class FakeLedger
    Implements IArrearsRecoveryLedger

    Public ReadOnly Items As New List(Of ArrearsRecoveryDue)()
    Public ReadOnly Reserved As New List(Of Integer)()
    Public ReadOnly Releases As New List(Of Integer)()
    Public ReserveFails As Boolean

    Public Function Due(runId As Integer) As IReadOnlyList(Of ArrearsRecoveryDue) Implements IArrearsRecoveryLedger.Due
        Return Items
    End Function

    Public Sub Reserve(runId As Integer, claimIds As IReadOnlyCollection(Of Integer)) Implements IArrearsRecoveryLedger.Reserve
        If ReserveFails Then Throw New PayrollRuleException("An asset recovery is not open. Process the run again.", 53515, Nothing)
        Reserved.AddRange(claimIds)
    End Sub

    Public Sub Release(runId As Integer) Implements IArrearsRecoveryLedger.Release
        Releases.Add(runId)
    End Sub
End Class

''' <summary>One regular run takes overpaid arrears and asset recoveries through a single ledger. Asset recovery ids are negated so the two kinds cannot meet.</summary>
Public Class AssetRecoveryLedgerTests
    Private ReadOnly _arrears As New FakeLedger()
    Private ReadOnly _assets As New FakeLedger()
    Private ReadOnly _both As CompositeRecoveryLedger

    Public Sub New()
        _both = New CompositeRecoveryLedger(_arrears, _assets)
    End Sub

    <Fact>
    Public Sub Due_ListsBoth_AssetIdsNegated()
        _arrears.Items.Add(New ArrearsRecoveryDue(7, 1, "E1", 500D))
        _assets.Items.Add(New ArrearsRecoveryDue(7, 2, "E2", 8000D))
        Dim due = _both.Due(3)
        Assert.Equal(2, due.Count)
        Assert.Equal(7, due.Single(Function(d) d.EmployeeId = 1).ClaimId)
        Assert.Equal(-7, due.Single(Function(d) d.EmployeeId = 2).ClaimId)
        Assert.Equal(8000D, due.Single(Function(d) d.EmployeeId = 2).Amount)
    End Sub

    <Fact>
    Public Sub Reserve_SendsEachIdToItsOwnLedger()
        _both.Reserve(3, New List(Of Integer) From {4, -9, 5, -2})
        Assert.Equal(New Integer() {4, 5}, _arrears.Reserved.OrderBy(Function(i) i).ToArray())
        Assert.Equal(New Integer() {2, 9}, _assets.Reserved.OrderBy(Function(i) i).ToArray())
    End Sub

    <Fact>
    Public Sub Reserve_OnlyOneKind_LeavesTheOtherLedgerAlone()
        _both.Reserve(3, New List(Of Integer) From {-9})
        Assert.Empty(_arrears.Reserved)
        Assert.Equal(9, _assets.Reserved.Single())
    End Sub

    <Fact>
    Public Sub Reserve_AssetsRefuse_PutsTheArrearsClaimsBack()
        _assets.ReserveFails = True
        Dim ex = Assert.Throws(Of PayrollRuleException)(Sub() _both.Reserve(3, New List(Of Integer) From {4, -9}))
        Assert.Contains("not open", ex.Message)
        Assert.Equal(3, _arrears.Releases.Single())
    End Sub

    <Fact>
    Public Sub Reserve_AssetsRefuseWithNoArrears_ReleasesNothing()
        _assets.ReserveFails = True
        Assert.Throws(Of PayrollRuleException)(Sub() _both.Reserve(3, New List(Of Integer) From {-9}))
        Assert.Empty(_arrears.Releases)
    End Sub

    <Fact>
    Public Sub Release_ReleasesBoth()
        _both.Release(3)
        Assert.Equal(3, _arrears.Releases.Single())
        Assert.Equal(3, _assets.Releases.Single())
    End Sub

    <Fact>
    Public Sub PlannerTakesBothKindsAsOneDeduction()
        _arrears.Items.Add(New ArrearsRecoveryDue(7, 1, "E1", 500D))
        _assets.Items.Add(New ArrearsRecoveryDue(7, 1, "E1", 8000D))
        Dim facts As New List(Of RunEmployeeFacts) From {RunFactsFor(1)}
        Dim plan = ArrearsRecoveryPlanner.Plan(facts, _both.Due(3))
        Assert.Equal(8500D, plan.Facts.Single().OtherDeductions)
        Assert.Equal(New Integer() {-7, 7}, plan.ClaimIds.OrderBy(Function(i) i).ToArray())
    End Sub

    Private Shared Function RunFactsFor(employeeId As Integer) As RunEmployeeFacts
        Return RunTestData.Facts(employeeId, "E" & employeeId.ToString(Globalization.CultureInfo.InvariantCulture))
    End Function
End Class
