''' <summary>Everything the run needs for one employee: pay type, statutory flags, the period's attendance, the salary in force, carried-in shortfall and open loan installments.</summary>
Public NotInheritable Class RunEmployeeFacts
    Public Sub New(employeeId As Integer, factoryId As Integer, empCode As String, isDailyWage As Boolean, genderId As Integer,
                   ptStateId As Integer?, isEpfMember As Boolean, epfOnHigherWage As Boolean, epsOnHigherWage As Boolean,
                   isEsiMember As Boolean, esiStayEligible As Boolean, paidDays As Decimal, overtimeHours As Decimal, hoursPerDay As Decimal?,
                   unmarkedDays As Integer, carryForwardIn As Decimal, otherDeductions As Decimal,
                   salary As IReadOnlyList(Of SalaryLineView), installments As IReadOnlyList(Of OpenInstallment))
        Me.EmployeeId = employeeId
        Me.FactoryId = factoryId
        Me.EmpCode = empCode
        Me.IsDailyWage = isDailyWage
        Me.GenderId = genderId
        Me.PtStateId = ptStateId
        Me.IsEpfMember = isEpfMember
        Me.EpfOnHigherWage = epfOnHigherWage
        Me.EpsOnHigherWage = epsOnHigherWage
        Me.IsEsiMember = isEsiMember
        Me.EsiStayEligible = esiStayEligible
        Me.PaidDays = paidDays
        Me.OvertimeHours = overtimeHours
        Me.HoursPerDay = hoursPerDay
        Me.UnmarkedDays = unmarkedDays
        Me.CarryForwardIn = carryForwardIn
        Me.OtherDeductions = otherDeductions
        Me.Salary = If(salary, New List(Of SalaryLineView)())
        Me.Installments = If(installments, New List(Of OpenInstallment)())
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property FactoryId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property IsDailyWage As Boolean
    ''' <summary>0 not stated, 1 male, 2 female (the professional tax slab codes).</summary>
    Public ReadOnly Property GenderId As Integer
    ''' <summary>State of the employee's factory when professional tax applies to the employee, otherwise Nothing.</summary>
    Public ReadOnly Property PtStateId As Integer?
    Public ReadOnly Property IsEpfMember As Boolean
    Public ReadOnly Property EpfOnHigherWage As Boolean
    Public ReadOnly Property EpsOnHigherWage As Boolean
    Public ReadOnly Property IsEsiMember As Boolean
    ''' <summary>The employee stays in ESI to the end of the contribution period. Not tracked yet: the data source returns False.</summary>
    Public ReadOnly Property EsiStayEligible As Boolean
    Public ReadOnly Property PaidDays As Decimal
    Public ReadOnly Property OvertimeHours As Decimal
    ''' <summary>Full-day hours of the employee's shift, or Nothing when no shift is assigned.</summary>
    Public ReadOnly Property HoursPerDay As Decimal?
    ''' <summary>Days inside the employee's working window of the period that have no attendance mark.</summary>
    Public ReadOnly Property UnmarkedDays As Integer
    Public ReadOnly Property CarryForwardIn As Decimal
    ''' <summary>No source yet. The data source returns 0.</summary>
    Public ReadOnly Property OtherDeductions As Decimal
    Public ReadOnly Property Salary As IReadOnlyList(Of SalaryLineView)
    Public ReadOnly Property Installments As IReadOnlyList(Of OpenInstallment)
End Class
