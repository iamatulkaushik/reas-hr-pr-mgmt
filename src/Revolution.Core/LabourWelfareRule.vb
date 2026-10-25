''' <summary>
''' A state's labour welfare fund rule in force on a date (stat.LabourWelfareRule, CR 0012). The employee share is a percentage of wages
''' (EmployeePct, with EmployeeAmount as the cap, 0 = no cap) or, when EmployeePct is 0, the flat EmployeeAmount. The employer share is
''' EmployerMultiple times the employee share, or the flat EmployerAmount when EmployerMultiple is 0. Amounts are rounded to 2 decimals,
''' MidpointRounding.AwayFromZero; whether the state rounds to whole rupees is for the CA to confirm. Call these only in a due month (IsDue).
''' </summary>
Public NotInheritable Class LabourWelfareRule
    Public Sub New(stateId As Integer, effectiveFrom As Date, employeePct As Decimal, employeeAmount As Decimal, employerMultiple As Decimal,
                   employerAmount As Decimal, frequency As LwfFrequency, wageCutoff As Decimal, dueMonths As IReadOnlyList(Of Integer),
                   verified As Boolean, verifiedOn As Date?, sourceRef As String)
        Me.StateId = stateId
        Me.EffectiveFrom = effectiveFrom
        Me.EmployeePct = employeePct
        Me.EmployeeAmount = employeeAmount
        Me.EmployerMultiple = employerMultiple
        Me.EmployerAmount = employerAmount
        Me.Frequency = frequency
        Me.WageCutoff = wageCutoff
        Me.DueMonths = If(dueMonths, New List(Of Integer)())
        Me.Verified = verified
        Me.VerifiedOn = verifiedOn
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    Public ReadOnly Property StateId As Integer
    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property EmployeePct As Decimal
    Public ReadOnly Property EmployeeAmount As Decimal
    Public ReadOnly Property EmployerMultiple As Decimal
    Public ReadOnly Property EmployerAmount As Decimal
    Public ReadOnly Property Frequency As LwfFrequency
    ''' <summary>An employee whose wage is above this pays nothing. Zero means no cutoff.</summary>
    Public ReadOnly Property WageCutoff As Decimal
    Public ReadOnly Property DueMonths As IReadOnlyList(Of Integer)
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property VerifiedOn As Date?
    Public ReadOnly Property SourceRef As String

    ''' <summary>True when the fund is deducted in the calendar month (1 to 12). A monthly rule is due every month.</summary>
    Public Function IsDue(month As Integer) As Boolean
        If month < 1 OrElse month > 12 Then
            Throw New ArgumentOutOfRangeException(NameOf(month))
        End If
        Return Frequency = LwfFrequency.Monthly OrElse DueMonths.Contains(month)
    End Function

    Public Function EmployeeContribution(monthlyWage As Decimal) As Decimal
        If monthlyWage <= 0D OrElse (WageCutoff > 0D AndAlso monthlyWage > WageCutoff) Then
            Return 0D
        End If
        If EmployeePct <= 0D Then
            Return EmployeeAmount
        End If
        Dim share = Math.Round(monthlyWage * EmployeePct / 100D, 2, MidpointRounding.AwayFromZero)
        Return If(EmployeeAmount > 0D AndAlso share > EmployeeAmount, EmployeeAmount, share)
    End Function

    Public Function EmployerContribution(monthlyWage As Decimal) As Decimal
        If monthlyWage <= 0D OrElse (WageCutoff > 0D AndAlso monthlyWage > WageCutoff) Then
            Return 0D
        End If
        If EmployerMultiple > 0D Then
            Return Math.Round(EmployeeContribution(monthlyWage) * EmployerMultiple, 2, MidpointRounding.AwayFromZero)
        End If
        Return EmployerAmount
    End Function
End Class
