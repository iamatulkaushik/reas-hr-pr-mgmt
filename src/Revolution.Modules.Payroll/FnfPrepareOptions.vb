''' <summary>The Owner's choices when preparing a settlement. BonusPct Nothing means the rule's minimum. EmployerEndedService turns notice pay from a recovery into an amount the employer pays. The last month's salary is not here: it is paid by the regular run.</summary>
Public NotInheritable Class FnfPrepareOptions
    Public Property BonusPct As Decimal?
    Public Property ArrearsRecovery As Decimal
    Public Property EmployerEndedService As Boolean
End Class
