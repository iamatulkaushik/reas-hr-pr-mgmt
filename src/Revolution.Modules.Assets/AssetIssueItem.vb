''' <summary>One hand-over of an asset, for its history. ReturnDate Nothing means the employee still holds it.</summary>
Public NotInheritable Class AssetIssueItem
    Public Sub New(issueId As Integer, employeeId As Integer, empCode As String, employeeName As String, issueDate As Date, issueCondition As String,
                   expectedReturnDate As Date?, returnDate As Date?, returnCondition As String)
        Me.IssueId = issueId
        Me.EmployeeId = employeeId
        Me.EmpCode = empCode
        Me.EmployeeName = employeeName
        Me.IssueDate = issueDate
        Me.IssueCondition = issueCondition
        Me.ExpectedReturnDate = expectedReturnDate
        Me.ReturnDate = returnDate
        Me.ReturnCondition = returnCondition
    End Sub

    Public ReadOnly Property IssueId As Integer
    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property EmployeeName As String
    Public ReadOnly Property IssueDate As Date
    Public ReadOnly Property IssueCondition As String
    Public ReadOnly Property ExpectedReturnDate As Date?
    Public ReadOnly Property ReturnDate As Date?
    Public ReadOnly Property ReturnCondition As String
End Class
