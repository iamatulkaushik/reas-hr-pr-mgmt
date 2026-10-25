''' <summary>Rent for a range of months as the Associate types it (Form 124). The landlord PAN is plain here; the service encrypts it before it reaches the repository.</summary>
Public NotInheritable Class TdsRentInput
    Public Sub New(fromMonthKey As Integer, toMonthKey As Integer, monthlyRent As Decimal, city As String, landlordName As String,
                   landlordRelationship As String, landlordAddress As String, landlordPan As String)
        Me.FromMonthKey = fromMonthKey
        Me.ToMonthKey = toMonthKey
        Me.MonthlyRent = monthlyRent
        Me.City = If(city, String.Empty).Trim()
        Me.LandlordName = If(landlordName, String.Empty).Trim()
        Me.LandlordRelationship = If(landlordRelationship, String.Empty).Trim()
        Me.LandlordAddress = landlordAddress
        Me.LandlordPan = landlordPan
    End Sub

    ''' <summary>YYYYMM.</summary>
    Public ReadOnly Property FromMonthKey As Integer
    Public ReadOnly Property ToMonthKey As Integer
    Public ReadOnly Property MonthlyRent As Decimal
    Public ReadOnly Property City As String
    Public ReadOnly Property LandlordName As String
    Public ReadOnly Property LandlordRelationship As String
    Public ReadOnly Property LandlordAddress As String
    Public ReadOnly Property LandlordPan As String
End Class
