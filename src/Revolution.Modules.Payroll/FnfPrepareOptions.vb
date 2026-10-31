''' <summary>The Owner's choices when preparing a settlement. BonusPct Nothing means the rule's minimum. LastSalaryOverride Nothing means the net pay of the run that covers the exit date.</summary>
Public NotInheritable Class FnfPrepareOptions
    Public Property BonusPct As Decimal?
    Public Property ArrearsRecovery As Decimal
    Public Property LastSalaryOverride As Decimal?
End Class
