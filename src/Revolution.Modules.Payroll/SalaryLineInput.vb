''' <summary>One salary head in force for the period, with its monthly figure.</summary>
Public NotInheritable Class SalaryLineInput
    Public Sub New(code As String, isBasic As Boolean, isDa As Boolean, isOtBase As Boolean, amount As Decimal)
        Me.Code = code
        Me.IsBasic = isBasic
        Me.IsDa = isDa
        Me.IsOtBase = isOtBase
        Me.Amount = amount
    End Sub

    Public ReadOnly Property Code As String
    Public ReadOnly Property IsBasic As Boolean
    Public ReadOnly Property IsDa As Boolean
    Public ReadOnly Property IsOtBase As Boolean
    ''' <summary>Monthly figure.</summary>
    Public ReadOnly Property Amount As Decimal

    ''' <summary>Basic and DA make the statutory base. Every other head is a wage exclusion.</summary>
    Public ReadOnly Property IsStatutoryBase As Boolean
        Get
            Return IsBasic OrElse IsDa
        End Get
    End Property
End Class
