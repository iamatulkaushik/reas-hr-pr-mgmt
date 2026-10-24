''' <summary>The salary in force on a date: the header and its lines.</summary>
Public NotInheritable Class EmployeeSalaryView
    Public Sub New(salaryId As Integer, effectiveFrom As Date, lines As IReadOnlyList(Of SalaryLineView))
        If lines Is Nothing Then Throw New ArgumentNullException(NameOf(lines))
        Me.SalaryId = salaryId
        Me.EffectiveFrom = effectiveFrom
        Me.Lines = lines
    End Sub

    Public ReadOnly Property SalaryId As Integer
    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property Lines As IReadOnlyList(Of SalaryLineView)

    ''' <summary>Monthly total of every line.</summary>
    Public ReadOnly Property MonthlyTotal As Decimal
        Get
            Return Lines.Sum(Function(l) l.Amount)
        End Get
    End Property
End Class
