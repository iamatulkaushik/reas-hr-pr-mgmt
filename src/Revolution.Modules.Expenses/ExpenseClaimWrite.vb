''' <summary>A claim to enter. BillNo needs a Vendor, so the same bill cannot be claimed twice.</summary>
Public NotInheritable Class ExpenseClaimWrite
    Public Property EmployeeId As Integer
    Public Property CategoryId As Integer
    Public Property ExpenseDate As Date
    Public Property Amount As Decimal
    Public Property BillNo As String
    Public Property BillDate As Date?
    Public Property Vendor As String
    Public Property Note As String
End Class
