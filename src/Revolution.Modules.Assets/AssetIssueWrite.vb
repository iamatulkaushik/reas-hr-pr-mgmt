''' <summary>Hand an asset to an employee.</summary>
Public NotInheritable Class AssetIssueWrite
    Public Property AssetId As Integer
    Public Property EmployeeId As Integer
    Public Property IssueDate As Date
    Public Property Condition As String
    Public Property ExpectedReturnDate As Date?
End Class
