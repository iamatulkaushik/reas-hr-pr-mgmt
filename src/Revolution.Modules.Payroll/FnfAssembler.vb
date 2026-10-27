Imports System.Globalization

''' <summary>
''' Turns the calculator results into the lines of a settlement, in a fixed order: salary, bonus, leave, gratuity, tax refund, then notice recovery, loan closure,
''' tax shortfall, arrears recovery. A zero amount adds no line. The lines are whole rupees because each calculator rounds once; nothing is rounded again here.
''' </summary>
Public NotInheritable Class FnfAssembler
    Private Sub New()
    End Sub

    Public Shared Function Build(input As FnfAssemblyInput) As IReadOnlyList(Of FnfLine)
        If input Is Nothing Then Throw New ArgumentNullException(NameOf(input))
        Dim lines As New List(Of FnfLine)()
        Add(lines, FnfComponentKind.LastSalary, "Salary to exit date", input.LastSalaryNet)
        If input.Bonus IsNot Nothing AndAlso input.Bonus.IsEligible Then Add(lines, FnfComponentKind.Bonus, "Bonus", input.Bonus.Amount)
        If input.Leave IsNot Nothing Then Add(lines, FnfComponentKind.LeaveEncashment, $"Leave encashment, {Days(input.Leave.Days)} days", input.Leave.Amount)
        If input.Gratuity IsNot Nothing AndAlso input.Gratuity.IsEligible Then Add(lines, FnfComponentKind.Gratuity, $"Gratuity, {input.Gratuity.CountedYears.ToString(CultureInfo.InvariantCulture)} years", input.Gratuity.Amount)
        If input.TdsAdjustment < 0D Then Add(lines, FnfComponentKind.TdsRefund, "Tax refund", -input.TdsAdjustment)
        If input.Notice IsNot Nothing Then Add(lines, FnfComponentKind.NoticePayRecovery, $"Notice pay recovery, {input.Notice.ShortDays.ToString(CultureInfo.InvariantCulture)} days short", input.Notice.Amount)
        Add(lines, FnfComponentKind.LoanClosure, "Loan closure", input.LoanOutstanding)
        If input.TdsAdjustment > 0D Then Add(lines, FnfComponentKind.TdsShortfall, "Tax shortfall", input.TdsAdjustment)
        Add(lines, FnfComponentKind.ArrearsRecovery, "Arrears recovery", input.ArrearsRecovery)
        Return lines
    End Function

    Private Shared Sub Add(lines As List(Of FnfLine), kind As FnfComponentKind, description As String, amount As Decimal)
        If amount > 0D Then lines.Add(New FnfLine(kind, description, amount))
    End Sub

    Private Shared Function Days(value As Decimal) As String
        Return value.ToString("0.##", CultureInfo.InvariantCulture)
    End Function
End Class
