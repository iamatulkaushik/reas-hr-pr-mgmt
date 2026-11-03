''' <summary>The arrears run that was made: its id and number, who is in it, and the claims left out with their reasons.</summary>
Public NotInheritable Class ArrearsRunResult
    Public Sub New(runId As Integer, runNo As Integer, employeeCount As Integer, claimCount As Integer, totalGross As Decimal, totalNet As Decimal, problems As IReadOnlyList(Of String))
        Me.RunId = runId
        Me.RunNo = runNo
        Me.EmployeeCount = employeeCount
        Me.ClaimCount = claimCount
        Me.TotalGross = totalGross
        Me.TotalNet = totalNet
        Me.Problems = problems
    End Sub

    Public ReadOnly Property RunId As Integer
    Public ReadOnly Property RunNo As Integer
    Public ReadOnly Property EmployeeCount As Integer
    Public ReadOnly Property ClaimCount As Integer
    Public ReadOnly Property TotalGross As Decimal
    Public ReadOnly Property TotalNet As Decimal
    Public ReadOnly Property Problems As IReadOnlyList(Of String)
End Class
