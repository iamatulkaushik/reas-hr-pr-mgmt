''' <summary>Employee fields as typed on the form, before anything is saved. Ids are plain text here and never logged.</summary>
Public NotInheritable Class EmployeeDraft
    Public Property FullName As String = String.Empty
    Public Property DateOfBirth As Date
    Public Property JoinDate As Date
    Public Property PayType As PayType = PayType.MonthlySalary
    Public Property Mobile As String = String.Empty
    Public Property Pan As String = String.Empty
    Public Property Aadhaar As String = String.Empty
    Public Property Uan As String = String.Empty
    Public Property EsiIp As String = String.Empty
    Public Property BankAccount As String = String.Empty
    Public Property Ifsc As String = String.Empty
End Class
