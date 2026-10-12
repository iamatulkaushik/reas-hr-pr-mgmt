''' <summary>How wages are defined for statutory contributions on a date. UseCodeWage means the 50 percent rule (Code on Wages).</summary>
Public NotInheritable Class WageRule
    Public Sub New(effectiveFrom As Date, useCodeWage As Boolean, wageFloorPct As Decimal, notes As String, verified As Boolean, sourceRef As String)
        Me.EffectiveFrom = effectiveFrom
        Me.UseCodeWage = useCodeWage
        Me.WageFloorPct = wageFloorPct
        Me.Notes = If(notes, String.Empty)
        Me.Verified = verified
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property UseCodeWage As Boolean
    Public ReadOnly Property WageFloorPct As Decimal
    Public ReadOnly Property Notes As String
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property SourceRef As String
End Class
