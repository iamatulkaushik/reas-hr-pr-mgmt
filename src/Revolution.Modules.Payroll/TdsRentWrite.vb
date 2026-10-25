''' <summary>A rent row ready to store: the landlord PAN is encrypted, with its last four characters for display (ADR 0011).</summary>
Public NotInheritable Class TdsRentWrite
    Public Sub New(rentNo As Integer, rent As TdsRentInput, panEnc As Byte(), panLast4 As String, keyVersion As Byte)
        Me.RentNo = rentNo
        Me.Rent = rent
        Me.PanEnc = panEnc
        Me.PanLast4 = panLast4
        Me.KeyVersion = keyVersion
    End Sub

    Public ReadOnly Property RentNo As Integer
    Public ReadOnly Property Rent As TdsRentInput
    Public ReadOnly Property PanEnc As Byte()
    Public ReadOnly Property PanLast4 As String
    Public ReadOnly Property KeyVersion As Byte
End Class
