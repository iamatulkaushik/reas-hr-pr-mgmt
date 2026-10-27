''' <summary>A saved settlement with its lines. MakerUserId and CheckerUserId are Nothing until someone calculates or approves.</summary>
Public NotInheritable Class FnfSettlementView
    Public Sub New(fnfId As Integer, employeeId As Integer, separationId As Integer, exitDate As Date, status As FnfStatus, totals As FnfTotals,
                   makerUserId As Integer?, checkerUserId As Integer?, lines As IReadOnlyList(Of FnfLine))
        Me.FnfId = fnfId
        Me.EmployeeId = employeeId
        Me.SeparationId = separationId
        Me.ExitDate = exitDate
        Me.Status = status
        Me.Totals = totals
        Me.MakerUserId = makerUserId
        Me.CheckerUserId = checkerUserId
        Me.Lines = lines
    End Sub

    Public ReadOnly Property FnfId As Integer
    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property SeparationId As Integer
    Public ReadOnly Property ExitDate As Date
    Public ReadOnly Property Status As FnfStatus
    Public ReadOnly Property Totals As FnfTotals
    Public ReadOnly Property MakerUserId As Integer?
    Public ReadOnly Property CheckerUserId As Integer?
    Public ReadOnly Property Lines As IReadOnlyList(Of FnfLine)
End Class
