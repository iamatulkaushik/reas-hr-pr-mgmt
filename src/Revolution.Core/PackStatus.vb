''' <summary>Where one line of the Monthly Pack stands.</summary>
Public Enum PackStatus
    ''' <summary>The payroll of the month is not approved or locked yet, or something needed is missing; the line says what.</summary>
    NotReady = 1
    ''' <summary>The figures are final and the file or summary can be made.</summary>
    Ready = 2
    ''' <summary>The file or summary was made; not yet filed or paid.</summary>
    Generated = 3
    ''' <summary>Filed on the portal, with the reference the Associate entered.</summary>
    Filed = 4
    ''' <summary>Paid, with the challan or payment reference the Associate entered.</summary>
    Paid = 5
    ''' <summary>Made earlier, but the figures of the month are different now (for example after a payroll reopen): make it again.</summary>
    Changed = 6
End Enum
