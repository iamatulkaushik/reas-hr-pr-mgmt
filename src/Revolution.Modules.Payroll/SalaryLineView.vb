''' <summary>One line of a salary in force, with the component's flags.</summary>
Public NotInheritable Class SalaryLineView
    Public Sub New(componentId As Integer, code As String, name As String, isBasic As Boolean, isDa As Boolean, isOtBase As Boolean, amount As Decimal)
        Me.ComponentId = componentId
        Me.Code = code
        Me.Name = name
        Me.IsBasic = isBasic
        Me.IsDa = isDa
        Me.IsOtBase = isOtBase
        Me.Amount = amount
    End Sub

    Public ReadOnly Property ComponentId As Integer
    Public ReadOnly Property Code As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property IsBasic As Boolean
    Public ReadOnly Property IsDa As Boolean
    Public ReadOnly Property IsOtBase As Boolean
    Public ReadOnly Property Amount As Decimal

    ''' <summary>The shape the wage builder takes.</summary>
    Public Function ToInput() As SalaryLineInput
        Return New SalaryLineInput(Code, IsBasic, IsDa, IsOtBase, Amount)
    End Function
End Class
