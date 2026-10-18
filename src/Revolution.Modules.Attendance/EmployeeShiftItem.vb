''' <summary>A shift assignment of one employee (att.EmployeeShift). EffectiveTo is Nothing for the open one.</summary>
Public NotInheritable Class EmployeeShiftItem
    Public Sub New(shiftId As Integer, shiftName As String, effectiveFrom As Date, effectiveTo As Date?)
        Me.ShiftId = shiftId
        Me.ShiftName = If(shiftName, String.Empty)
        Me.EffectiveFrom = effectiveFrom.Date
        Me.EffectiveTo = effectiveTo
    End Sub

    Public ReadOnly Property ShiftId As Integer
    Public ReadOnly Property ShiftName As String
    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property EffectiveTo As Date?
End Class
