''' <summary>An expense category to add (CategoryId Nothing) or change.</summary>
Public NotInheritable Class ExpenseCategoryWrite
    Public Property CategoryId As Integer?
    Public Property Name As String
    Public Property PerClaimLimit As Decimal?
    Public Property PerMonthLimit As Decimal?
    Public Property IsActive As Boolean = True
End Class
