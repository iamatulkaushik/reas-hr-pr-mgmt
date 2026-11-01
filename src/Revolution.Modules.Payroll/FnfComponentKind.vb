''' <summary>What a full and final line is. The numbers match pay.FnfComponent.ComponentTypeID.</summary>
Public Enum FnfComponentKind
    ''' <summary>Not used since 2026-11-01: the last month's salary is paid by the regular run. The number stays because the database check allows it.</summary>
    LastSalary = 1
    NoticePayRecovery = 2
    Bonus = 3
    LeaveEncashment = 4
    Gratuity = 5
    LoanClosure = 6
    TdsShortfall = 7
    TdsRefund = 8
    ArrearsRecovery = 9
    OtherEarning = 10
    OtherDeduction = 11
End Enum
