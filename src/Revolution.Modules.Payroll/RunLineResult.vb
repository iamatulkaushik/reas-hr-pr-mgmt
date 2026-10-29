''' <summary>One employee's line of a payroll run, with the same columns as pay.PayrollRunLine. Earnings is empty when the line is read back from the database.</summary>
Public NotInheritable Class RunLineResult
    Public Sub New(employeeId As Integer, factoryId As Integer, paidDays As Decimal, overtimeHours As Decimal, overtimeAmount As Decimal,
                   grossEarnings As Decimal, epfWage As Decimal, esiWage As Decimal, epfEmployee As Decimal, epfEmployerEpf As Decimal,
                   epfEmployerEps As Decimal, edli As Decimal, epfAdmin As Decimal, esiEmployee As Decimal, esiEmployer As Decimal,
                   professionalTax As Decimal, loanEmi As Decimal, loanEmiDeferred As Decimal, otherDeductions As Decimal,
                   carryForwardIn As Decimal, carryForwardOut As Decimal, totalDeductions As Decimal, netPay As Decimal,
                   earnings As IReadOnlyList(Of EarningAmount), Optional tdsAmount As Decimal = 0D, Optional tdsShortfall As Decimal = 0D)
        Me.TdsAmount = tdsAmount
        Me.TdsShortfall = tdsShortfall
        Me.EmployeeId = employeeId
        Me.FactoryId = factoryId
        Me.PaidDays = paidDays
        Me.OvertimeHours = overtimeHours
        Me.OvertimeAmount = overtimeAmount
        Me.GrossEarnings = grossEarnings
        Me.EpfWage = epfWage
        Me.EsiWage = esiWage
        Me.EpfEmployee = epfEmployee
        Me.EpfEmployerEpf = epfEmployerEpf
        Me.EpfEmployerEps = epfEmployerEps
        Me.Edli = edli
        Me.EpfAdmin = epfAdmin
        Me.EsiEmployee = esiEmployee
        Me.EsiEmployer = esiEmployer
        Me.ProfessionalTax = professionalTax
        Me.LoanEmi = loanEmi
        Me.LoanEmiDeferred = loanEmiDeferred
        Me.OtherDeductions = otherDeductions
        Me.CarryForwardIn = carryForwardIn
        Me.CarryForwardOut = carryForwardOut
        Me.TotalDeductions = totalDeductions
        Me.NetPay = netPay
        Me.Earnings = If(earnings, New List(Of EarningAmount)())
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property FactoryId As Integer
    Public ReadOnly Property PaidDays As Decimal
    Public ReadOnly Property OvertimeHours As Decimal
    Public ReadOnly Property OvertimeAmount As Decimal
    Public ReadOnly Property GrossEarnings As Decimal
    Public ReadOnly Property EpfWage As Decimal
    Public ReadOnly Property EsiWage As Decimal
    Public ReadOnly Property EpfEmployee As Decimal
    Public ReadOnly Property EpfEmployerEpf As Decimal
    Public ReadOnly Property EpfEmployerEps As Decimal
    Public ReadOnly Property Edli As Decimal
    Public ReadOnly Property EpfAdmin As Decimal
    Public ReadOnly Property EsiEmployee As Decimal
    Public ReadOnly Property EsiEmployer As Decimal
    Public ReadOnly Property ProfessionalTax As Decimal
    ''' <summary>Income tax deducted from this pay (CR 0019). Zero when the plan has no TDS feature.</summary>
    Public ReadOnly Property TdsAmount As Decimal
    ''' <summary>Tax due that pay could not cover. Shown as a warning on the line; not stored, so it is zero when the line is read back.</summary>
    Public ReadOnly Property TdsShortfall As Decimal
    ''' <summary>EMI taken from this pay.</summary>
    Public ReadOnly Property LoanEmi As Decimal
    ''' <summary>Scheduled EMI not taken (cap or short pay). It is scheduled again in the next run.</summary>
    Public ReadOnly Property LoanEmiDeferred As Decimal
    Public ReadOnly Property OtherDeductions As Decimal
    Public ReadOnly Property CarryForwardIn As Decimal
    Public ReadOnly Property CarryForwardOut As Decimal
    ''' <summary>Gross less net pay: statutory, income tax, other deductions, carried-in shortfall and EMI taken.</summary>
    Public ReadOnly Property TotalDeductions As Decimal
    Public ReadOnly Property NetPay As Decimal
    Public ReadOnly Property Earnings As IReadOnlyList(Of EarningAmount)
End Class
