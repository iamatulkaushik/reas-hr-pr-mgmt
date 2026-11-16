''' <summary>The statutory lines of one company for one month, in the order the Associate works through them.</summary>
Public NotInheritable Class MonthlyPack
    Public Sub New(companyName As String, periodStart As Date, lines As IReadOnlyList(Of MonthlyPackLine), payrollMessage As String)
        If lines Is Nothing Then Throw New ArgumentNullException(NameOf(lines))
        Me.CompanyName = If(companyName, String.Empty)
        Me.PeriodStart = periodStart
        Me.Lines = lines
        Me.PayrollMessage = If(payrollMessage, String.Empty)
    End Sub

    Public ReadOnly Property CompanyName As String
    ''' <summary>The first day of the month.</summary>
    Public ReadOnly Property PeriodStart As Date
    Public ReadOnly Property Lines As IReadOnlyList(Of MonthlyPackLine)
    ''' <summary>Empty when the payroll of the month is approved or locked; otherwise the reason the lines are not ready.</summary>
    Public ReadOnly Property PayrollMessage As String

    Public ReadOnly Property ReadyCount As Integer
        Get
            Dim count As Integer = 0
            For Each line In Lines
                If line.Status = PackStatus.Ready OrElse line.Status = PackStatus.Changed Then
                    count += 1
                End If
            Next
            Return count
        End Get
    End Property
End Class
