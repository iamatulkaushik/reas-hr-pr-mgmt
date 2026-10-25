''' <summary>One month of HRA facts for an employee: salary for HRA (basic plus DA), HRA received, rent paid and the city lived in that month.</summary>
Public NotInheritable Class HraMonthInput
    Public Sub New(month As Date, basicPlusDa As Decimal, hraReceived As Decimal, rentPaid As Decimal, city As String)
        If basicPlusDa < 0D OrElse hraReceived < 0D OrElse rentPaid < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(basicPlusDa), "Salary, HRA and rent cannot be negative.")
        End If
        Me.Month = month
        Me.BasicPlusDa = basicPlusDa
        Me.HraReceived = hraReceived
        Me.RentPaid = rentPaid
        Me.City = If(city, String.Empty)
    End Sub

    Public ReadOnly Property Month As Date
    Public ReadOnly Property BasicPlusDa As Decimal
    Public ReadOnly Property HraReceived As Decimal
    Public ReadOnly Property RentPaid As Decimal
    Public ReadOnly Property City As String
End Class
