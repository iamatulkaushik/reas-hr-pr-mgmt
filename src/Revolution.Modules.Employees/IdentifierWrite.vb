''' <summary>The four employee ids as they go to mst.usp_EmployeeStatutory_SetIdentifiers. One key version covers the whole row.</summary>
Public NotInheritable Class IdentifierWrite
    Public Sub New(keyVersion As Byte, uan As IdChange, esiIp As IdChange, pan As IdChange, aadhaar As IdChange)
        If uan Is Nothing Then Throw New ArgumentNullException(NameOf(uan))
        If esiIp Is Nothing Then Throw New ArgumentNullException(NameOf(esiIp))
        If pan Is Nothing Then Throw New ArgumentNullException(NameOf(pan))
        If aadhaar Is Nothing Then Throw New ArgumentNullException(NameOf(aadhaar))
        Me.KeyVersion = keyVersion
        Me.Uan = uan
        Me.EsiIp = esiIp
        Me.Pan = pan
        Me.Aadhaar = aadhaar
    End Sub

    Public ReadOnly Property KeyVersion As Byte
    Public ReadOnly Property Uan As IdChange
    Public ReadOnly Property EsiIp As IdChange
    Public ReadOnly Property Pan As IdChange
    Public ReadOnly Property Aadhaar As IdChange
End Class
