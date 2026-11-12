''' <summary>What a recovery would be today: the asset, the employee who held it, and its depreciated value on AsOf (the default amount).</summary>
Public NotInheritable Class AssetRecoveryQuote
    Public Sub New(source As AssetRecoverySource, depreciatedValue As Decimal, asOf As Date)
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        Me.Source = source
        Me.DepreciatedValue = depreciatedValue
        Me.AsOf = asOf.Date
    End Sub

    Public ReadOnly Property Source As AssetRecoverySource
    Public ReadOnly Property DepreciatedValue As Decimal
    Public ReadOnly Property AsOf As Date
End Class
