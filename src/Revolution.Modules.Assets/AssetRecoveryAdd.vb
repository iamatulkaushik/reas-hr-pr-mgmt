''' <summary>A recovery to record: the depreciated value on BasisDate and the amount the Owner decided (the same unless a Reason is given).</summary>
Public NotInheritable Class AssetRecoveryAdd
    Public Property AssetId As Integer
    Public Property BasisDate As Date
    Public Property DepreciatedValue As Decimal
    Public Property Amount As Decimal
    Public Property Reason As String
End Class
