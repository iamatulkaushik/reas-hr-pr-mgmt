''' <summary>One row of a state's minimum wage table (stat.MinimumWage, CR 0012). The amount is the notified basic rate: compare it with the whole basic wage, not with parts of it.</summary>
Public NotInheritable Class MinimumWageRate
    Public Sub New(stateId As Integer, effectiveFrom As Date, zoneNo As Integer, skill As WageSkill, unit As WageUnit, amount As Decimal,
                   verified As Boolean, verifiedOn As Date?, sourceRef As String)
        Me.StateId = stateId
        Me.EffectiveFrom = effectiveFrom
        Me.ZoneNo = zoneNo
        Me.Skill = skill
        Me.Unit = unit
        Me.Amount = amount
        Me.Verified = verified
        Me.VerifiedOn = verifiedOn
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    Public ReadOnly Property StateId As Integer
    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property ZoneNo As Integer
    Public ReadOnly Property Skill As WageSkill
    Public ReadOnly Property Unit As WageUnit
    Public ReadOnly Property Amount As Decimal
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property VerifiedOn As Date?
    Public ReadOnly Property SourceRef As String
End Class
