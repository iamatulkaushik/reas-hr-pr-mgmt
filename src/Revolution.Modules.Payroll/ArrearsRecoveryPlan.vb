''' <summary>What a regular run takes back for overpaid arrears: the facts with the recovery added to each employee's other deductions, the claims that go with it, and the employees who could not be reached.</summary>
Public NotInheritable Class ArrearsRecoveryPlan
    Public Sub New(facts As IReadOnlyList(Of RunEmployeeFacts), claimIds As IReadOnlyList(Of Integer), notInRun As IReadOnlyList(Of String))
        If facts Is Nothing Then Throw New ArgumentNullException(NameOf(facts))
        If claimIds Is Nothing Then Throw New ArgumentNullException(NameOf(claimIds))
        If notInRun Is Nothing Then Throw New ArgumentNullException(NameOf(notInRun))
        Me.Facts = facts
        Me.ClaimIds = claimIds
        Me.NotInRun = notInRun
    End Sub

    Public ReadOnly Property Facts As IReadOnlyList(Of RunEmployeeFacts)
    ''' <summary>The claims the run takes back, in id order.</summary>
    Public ReadOnly Property ClaimIds As IReadOnlyList(Of Integer)
    ''' <summary>Codes of employees with an overpaid claim who have no pay in the period. Their claims stay Open for a later run.</summary>
    Public ReadOnly Property NotInRun As IReadOnlyList(Of String)
End Class
