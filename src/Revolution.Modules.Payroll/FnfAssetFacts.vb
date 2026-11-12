''' <summary>An employee's assets as the settlement sees them: the total of the open recoveries (or those this settlement holds) and the tags of assets not cleared yet.</summary>
Public NotInheritable Class FnfAssetFacts
    Public Sub New(recoveryTotal As Decimal, uncleared As IReadOnlyList(Of String))
        If recoveryTotal < 0D Then Throw New ArgumentOutOfRangeException(NameOf(recoveryTotal), "The recovery total cannot be negative.")
        Me.RecoveryTotal = recoveryTotal
        Me.Uncleared = If(uncleared, New List(Of String)())
    End Sub

    ''' <summary>Whole rupees to take on the settlement.</summary>
    Public ReadOnly Property RecoveryTotal As Decimal
    ''' <summary>Tags of assets the employee still holds that have no recovery and are not written off. They block the settlement.</summary>
    Public ReadOnly Property Uncleared As IReadOnlyList(Of String)
End Class
