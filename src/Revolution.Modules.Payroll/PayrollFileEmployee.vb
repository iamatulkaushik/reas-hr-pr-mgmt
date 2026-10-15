''' <summary>
''' One employee, ready for the statutory and bank files: run line figures plus the decrypted identity details.
''' The caller decrypts UAN, ESI number and account in the app. This module never touches the database or keys.
''' </summary>
Public NotInheritable Class PayrollFileEmployee
    Public Property EmployeeID As Integer
    Public Property Name As String = String.Empty
    Public Property PaidDays As Decimal
    Public Property GrossEarnings As Decimal
    Public Property EpfWage As Decimal
    Public Property EsiWage As Decimal
    Public Property EpfEmployee As Decimal
    Public Property EpfEmployerEpf As Decimal
    Public Property EpfEmployerEps As Decimal
    Public Property Edli As Decimal
    Public Property NetPay As Decimal
    Public Property IsEpfMember As Boolean
    Public Property Uan As String = String.Empty
    Public Property IsEsiMember As Boolean
    Public Property EsiIpNumber As String = String.Empty
    ''' <summary>True when the employee is paid into a bank account.</summary>
    Public Property PaysByBank As Boolean
    Public Property Ifsc As String = String.Empty
    Public Property AccountNumber As String = String.Empty
    ''' <summary>ESIC reason code for zero days. Empty means 0, Without Reason.</summary>
    Public Property EsiReasonCode As Integer?
    Public Property LastWorkingDay As Date?
End Class
