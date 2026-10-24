Imports Revolution.Core

''' <summary>Checks policy, component and salary input before it reaches the database. Limits mirror the pay.* column types and checks.</summary>
Public NotInheritable Class SalaryValidator
    Public Const MaxCodeLength As Integer = 20
    Public Const MaxNameLength As Integer = 100
    Public Const MaxReasonLength As Integer = 200
    Public Const MaxAmount As Decimal = 999999999999.99D
    Public Const MaxOtMultiplier As Decimal = 10D
    Public Const MaxDailyWageDivisor As Decimal = 31D
    Public Shared ReadOnly EarliestEffectiveDate As Date = New Date(2000, 1, 1)

    Private Sub New()
    End Sub

    Public Shared Function ValidatePolicy(policy As PolicySettings) As OperationResult
        If policy Is Nothing Then Return OperationResult.Fail("Policy is missing.")
        If policy.PayCycleStartDay <> 1 AndAlso policy.PayCycleStartDay <> 26 Then
            Return OperationResult.Fail("Pay cycle starts on the 1st or the 26th.")
        End If
        If policy.OtMultiplier < 1D OrElse policy.OtMultiplier > MaxOtMultiplier Then
            Return OperationResult.Fail("Overtime multiplier must be between 1 and 10.")
        End If
        If policy.DailyWageDivisor <= 0D OrElse policy.DailyWageDivisor > MaxDailyWageDivisor Then
            Return OperationResult.Fail("Daily wage divisor must be more than 0 and at most 31.")
        End If
        If policy.EmiCapPct < 0D OrElse policy.EmiCapPct > 100D Then
            Return OperationResult.Fail("EMI cap must be between 0 and 100 percent.")
        End If
        Return OperationResult.Ok()
    End Function

    Public Shared Function ValidateComponent(component As SalaryComponentItem) As OperationResult
        If component Is Nothing Then Return OperationResult.Fail("Component is missing.")
        If String.IsNullOrWhiteSpace(component.Code) Then Return OperationResult.Fail("Component code is required.")
        If component.Code.Trim().Length > MaxCodeLength Then Return OperationResult.Fail("Component code is too long (20 characters at most).")
        If String.IsNullOrWhiteSpace(component.Name) Then Return OperationResult.Fail("Component name is required.")
        If component.Name.Trim().Length > MaxNameLength Then Return OperationResult.Fail("Component name is too long (100 characters at most).")
        If component.IsBasic AndAlso component.IsDa Then Return OperationResult.Fail("A component cannot be both basic and DA.")
        If component.SortOrder < 0 OrElse component.SortOrder > Short.MaxValue Then Return OperationResult.Fail("Sort order is not valid.")
        If component.ComponentId.HasValue AndAlso component.ComponentId.Value <= 0 Then Return OperationResult.Fail("Component is not valid.")
        Return OperationResult.Ok()
    End Function

    ''' <summary>
    ''' A salary revision needs an employee, a date, at least one line, each component once and active, amounts of at most two decimals,
    ''' and a basic or DA amount above zero (the statutory base).
    ''' </summary>
    Public Shared Function ValidateSalary(employeeId As Integer, effectiveFrom As Date, reason As String,
                                          lines As IReadOnlyList(Of SalaryLineWrite),
                                          components As IReadOnlyList(Of SalaryComponentItem)) As OperationResult
        If employeeId <= 0 Then Return OperationResult.Fail("Pick an employee.")
        If effectiveFrom < EarliestEffectiveDate Then Return OperationResult.Fail("Effective date is not valid.")
        If reason IsNot Nothing AndAlso reason.Trim().Length > MaxReasonLength Then Return OperationResult.Fail("Reason is too long (200 characters at most).")
        If lines Is Nothing OrElse lines.Count = 0 Then Return OperationResult.Fail("Salary needs at least one line.")
        Dim byId As New Dictionary(Of Integer, SalaryComponentItem)()
        For Each c As SalaryComponentItem In components
            If c.ComponentId.HasValue Then byId(c.ComponentId.Value) = c
        Next
        Dim seen As New HashSet(Of Integer)()
        Dim statutoryBase As Decimal = 0D
        For Each line As SalaryLineWrite In lines
            If Not seen.Add(line.ComponentId) Then Return OperationResult.Fail("A component is listed twice.")
            Dim component As SalaryComponentItem = Nothing
            If Not byId.TryGetValue(line.ComponentId, component) Then Return OperationResult.Fail("A component on the salary does not exist.")
            If Not component.IsActive Then Return OperationResult.Fail($"Component {component.Code} is not active.")
            If line.Amount < 0D Then Return OperationResult.Fail($"Amount for {component.Code} cannot be negative.")
            If line.Amount > MaxAmount Then Return OperationResult.Fail($"Amount for {component.Code} is too large.")
            If Decimal.Round(line.Amount, 2) <> line.Amount Then Return OperationResult.Fail($"Amount for {component.Code} has more than two decimals.")
            If component.IsBasic OrElse component.IsDa Then statutoryBase += line.Amount
        Next
        If statutoryBase <= 0D Then Return OperationResult.Fail("Salary needs a basic or DA amount above zero.")
        Return OperationResult.Ok()
    End Function
End Class
