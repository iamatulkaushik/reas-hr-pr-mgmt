''' <summary>
''' Everything the settlement is built from, each already worked out by its own calculator. A result that is Nothing, or not eligible, adds no line. The last
''' month's salary is not here: the regular run pays it. NoticePayable is notice pay the employer owes when it ended the service early. TdsAdjustment is the final tax true-up: positive when more tax is due, negative when tax is to be refunded.
''' ArrearsRecovery is the Owner's option on the deduction side and is zero unless the Owner chooses to recover.
''' </summary>
Public NotInheritable Class FnfAssemblyInput
    Public Sub New(Optional notice As NoticePayResult = Nothing, Optional bonus As BonusResult = Nothing,
                   Optional leave As LeaveEncashmentResult = Nothing, Optional gratuity As GratuityResult = Nothing, Optional loanOutstanding As Decimal = 0D,
                   Optional tdsAdjustment As Decimal = 0D, Optional arrearsRecovery As Decimal = 0D, Optional noticePayable As NoticePayResult = Nothing)
        If loanOutstanding < 0D Then Throw New ArgumentOutOfRangeException(NameOf(loanOutstanding), "The loan outstanding cannot be negative.")
        If arrearsRecovery < 0D Then Throw New ArgumentOutOfRangeException(NameOf(arrearsRecovery), "The arrears recovery cannot be negative.")
        Me.Notice = notice
        Me.NoticePayable = noticePayable
        Me.Bonus = bonus
        Me.Leave = leave
        Me.Gratuity = gratuity
        Me.LoanOutstanding = loanOutstanding
        Me.TdsAdjustment = tdsAdjustment
        Me.ArrearsRecovery = arrearsRecovery
    End Sub

    Public ReadOnly Property Notice As NoticePayResult
    Public ReadOnly Property NoticePayable As NoticePayResult
    Public ReadOnly Property Bonus As BonusResult
    Public ReadOnly Property Leave As LeaveEncashmentResult
    Public ReadOnly Property Gratuity As GratuityResult
    Public ReadOnly Property LoanOutstanding As Decimal
    Public ReadOnly Property TdsAdjustment As Decimal
    Public ReadOnly Property ArrearsRecovery As Decimal
End Class
