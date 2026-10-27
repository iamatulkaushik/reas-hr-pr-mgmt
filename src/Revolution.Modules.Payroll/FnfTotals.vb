''' <summary>Totals of a settlement. Net is earnings less deductions and is negative when the employee owes the company.</summary>
Public NotInheritable Class FnfTotals
    Public Sub New(earnings As Decimal, deductions As Decimal)
        Me.Earnings = earnings
        Me.Deductions = deductions
    End Sub

    Public ReadOnly Property Earnings As Decimal
    Public ReadOnly Property Deductions As Decimal
    Public ReadOnly Property Net As Decimal
        Get
            Return Earnings - Deductions
        End Get
    End Property
    ''' <summary>True when the deductions are more than the earnings, so the employee owes money.</summary>
    Public ReadOnly Property IsRecoverable As Boolean
        Get
            Return Net < 0D
        End Get
    End Property
End Class
