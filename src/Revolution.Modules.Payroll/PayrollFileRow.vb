''' <summary>One run line with the plain employee facts the files need. Holds no id: those are released separately.</summary>
Public NotInheritable Class PayrollFileRow
    Public Property EmployeeID As Integer
    Public Property EmployeeCode As String = String.Empty
    Public Property Name As String = String.Empty
    Public Property PaidDays As Decimal
    Public Property GrossEarnings As Decimal
    Public Property EpfWage As Decimal
    Public Property EsiWage As Decimal
    Public Property EpfEmployee As Decimal
    Public Property EpfEmployerEpf As Decimal
    Public Property EpfEmployerEps As Decimal
    Public Property Edli As Decimal
    Public Property EsiEmployee As Decimal
    Public Property EsiEmployer As Decimal
    Public Property NetPay As Decimal
    Public Property IsEpfMember As Boolean
    Public Property EpsOnHigherWage As Boolean
    Public Property IsEsiMember As Boolean
    Public Property PaysByBank As Boolean
    Public Property Ifsc As String = String.Empty
    ''' <summary>True when an account number is on file. The number itself is never read here.</summary>
    Public Property HasAccount As Boolean
    Public Property ExitDate As Date?
End Class
