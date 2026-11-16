''' <summary>A stored rent row for screens: the landlord PAN shows its last four characters only.</summary>
Public NotInheritable Class TdsRentView
    Public Sub New(rentNo As Integer, fromMonthKey As Integer, toMonthKey As Integer, monthlyRent As Decimal, city As String, landlordName As String,
                   landlordRelationship As String, landlordAddress As String, landlordPanLast4 As String,
                   Optional panOption As TdsLandlordPanOption = TdsLandlordPanOption.PanGiven)
        Me.RentNo = rentNo
        Me.FromMonthKey = fromMonthKey
        Me.ToMonthKey = toMonthKey
        Me.MonthlyRent = monthlyRent
        Me.City = city
        Me.LandlordName = landlordName
        Me.LandlordRelationship = landlordRelationship
        Me.LandlordAddress = landlordAddress
        Me.LandlordPanLast4 = landlordPanLast4
        Me.PanOption = panOption
    End Sub

    Public ReadOnly Property RentNo As Integer
    Public ReadOnly Property FromMonthKey As Integer
    Public ReadOnly Property ToMonthKey As Integer
    Public ReadOnly Property MonthlyRent As Decimal
    Public ReadOnly Property City As String
    Public ReadOnly Property LandlordName As String
    Public ReadOnly Property LandlordRelationship As String
    Public ReadOnly Property LandlordAddress As String
    Public ReadOnly Property LandlordPanLast4 As String
    Public ReadOnly Property PanOption As TdsLandlordPanOption
    ''' <summary>False when the employee chose not to claim the HRA exemption for this row.</summary>
    Public ReadOnly Property ExemptionAllowed As Boolean
        Get
            Return PanOption <> TdsLandlordPanOption.ExemptionNotClaimed
        End Get
    End Property
    Public ReadOnly Property HasPan As Boolean
        Get
            Return Not String.IsNullOrEmpty(LandlordPanLast4)
        End Get
    End Property
End Class
