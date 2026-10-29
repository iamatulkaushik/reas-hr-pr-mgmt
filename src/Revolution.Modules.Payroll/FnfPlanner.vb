Imports Revolution.Core

''' <summary>
''' Builds a settlement from the facts, using each calculator once: notice pay recovery, bonus, leave encashment, gratuity, then the assembler and the totals. Pure: no database,
''' no permissions. A line that does not apply adds a note, not an error. Bad facts (exit before joining, a bonus percentage outside the rule's range) fail the plan with a message.
''' </summary>
Public NotInheritable Class FnfPlanner
    Private Sub New()
    End Sub

    Public Shared Function Plan(facts As FnfFacts) As OperationResult(Of FnfPlan)
        If facts Is Nothing Then Throw New ArgumentNullException(NameOf(facts))
        Dim problem As String = Check(facts)
        If problem IsNot Nothing Then Return OperationResult(Of FnfPlan).Fail(problem)
        Dim notes As New List(Of String)()
        Dim input As New FnfAssemblyInput(facts.LastSalaryNet, Notice(facts, notes), Bonus(facts, notes), Leave(facts, notes), Gratuity(facts, notes),
                                          facts.LoanOutstanding, facts.TdsAdjustment, facts.ArrearsRecovery)
        Dim lines As IReadOnlyList(Of FnfLine) = FnfAssembler.Build(input)
        Return OperationResult(Of FnfPlan).Ok(New FnfPlan(lines, FnfCalculator.Total(lines), FnfDueWindow.For(facts.LastWorkingDay), notes))
    End Function

    Private Shared Function Check(facts As FnfFacts) As String
        If facts.LastWorkingDay.Date < facts.JoinDate.Date Then Return "The last working day is before the joining date."
        If facts.LastSalaryNet < 0D OrElse facts.DailyWageGross < 0D OrElse facts.LoanOutstanding < 0D OrElse facts.ArrearsRecovery < 0D OrElse facts.LeaveBalanceDays < 0D Then Return "Money and days cannot be negative."
        If facts.NoticePeriodDays < 0 Then Return "Notice days cannot be negative."
        If facts.BonusRule IsNot Nothing AndAlso (facts.BonusPct < facts.BonusRule.MinPct OrElse facts.BonusPct > facts.BonusRule.MaxPct) Then
            Return $"The bonus must be between {facts.BonusRule.MinPct}% and {facts.BonusRule.MaxPct}%."
        End If
        Return Nothing
    End Function

    Private Shared Function Notice(facts As FnfFacts, notes As List(Of String)) As NoticePayResult
        If Not facts.NoticeApplies Then
            notes.Add("Notice pay is recovered only when the employee resigned.")
            Return Nothing
        End If
        Dim served As Integer = NoticePayCalculator.ServedDays(facts.NoticeDate, facts.LastWorkingDay)
        Return NoticePayCalculator.Calculate(facts.NoticePeriodDays, served, facts.DailyWageGross)
    End Function

    Private Shared Function Bonus(facts As FnfFacts, notes As List(Of String)) As BonusResult
        If facts.BonusRule Is Nothing Then
            notes.Add("No bonus rule is in force for the exit date, so no bonus is paid.")
            Return Nothing
        End If
        Dim result As BonusResult = BonusCalculator.Calculate(facts.BonusRule, facts.BonusLastMonthlyWage, facts.BonusMonthlyWages, facts.BonusDaysWorked, facts.BonusPct, facts.BonusAlreadyPaid)
        If Not result.IsEligible Then notes.Add("Bonus not paid: " & result.Reason)
        Return result
    End Function

    Private Shared Function Leave(facts As FnfFacts, notes As List(Of String)) As LeaveEncashmentResult
        If Not facts.LeaveEncashable Then
            notes.Add("This leave type cannot be encashed.")
            Return Nothing
        End If
        Return LeaveEncashmentCalculator.Calculate(facts.LeaveBalanceDays, facts.LeaveMaxEncashDays, facts.DailyWageGross)
    End Function

    Private Shared Function Gratuity(facts As FnfFacts, notes As List(Of String)) As GratuityResult
        If Not facts.GratuityCovered Then
            notes.Add("Gratuity does not apply to this employment type.")
            Return Nothing
        End If
        If facts.GratuityRule Is Nothing Then
            notes.Add("No gratuity rule is in force for the exit date, so no gratuity is paid.")
            Return Nothing
        End If
        Dim result As GratuityResult = GratuityCalculator.Calculate(facts.JoinDate, facts.LastWorkingDay, facts.GratuityWage, facts.IsFixedTerm, facts.WaiveGratuityMinimum, facts.GratuityRule)
        If Not result.IsEligible Then notes.Add("Gratuity not paid: " & result.Reason)
        Return result
    End Function
End Class
