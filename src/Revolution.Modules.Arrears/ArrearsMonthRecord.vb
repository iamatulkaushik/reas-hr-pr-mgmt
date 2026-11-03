Imports Revolution.Modules.Payroll

''' <summary>One wage month of a stored claim: the earnings paid and due, and the contributions on the difference. Signed whole rupees.</summary>
Public NotInheritable Class ArrearsMonthRecord
    Public Sub New(wageMonth As Date, paidGross As Decimal, dueGross As Decimal, grossDifference As Decimal, epfWages As Decimal, epsWages As Decimal, edliWages As Decimal,
                   employeeEpf As Decimal, employerEpf As Decimal, employerEps As Decimal, edli As Decimal, admin As Decimal, esiEmployee As Decimal, esiEmployer As Decimal,
                   Optional heads As IReadOnlyList(Of ArrearsHeadAmount) = Nothing, Optional overtimeDifference As Decimal = 0D, Optional esiWages As Decimal = 0D)
        Me.WageMonth = wageMonth
        Me.PaidGross = paidGross
        Me.DueGross = dueGross
        Me.GrossDifference = grossDifference
        Me.EpfWages = epfWages
        Me.EpsWages = epsWages
        Me.EdliWages = edliWages
        Me.EmployeeEpf = employeeEpf
        Me.EmployerEpf = employerEpf
        Me.EmployerEps = employerEps
        Me.Edli = edli
        Me.Admin = admin
        Me.EsiEmployee = esiEmployee
        Me.EsiEmployer = esiEmployer
        Me.Heads = If(heads, New List(Of ArrearsHeadAmount)())
        Me.OvertimeDifference = overtimeDifference
        Me.EsiWages = esiWages
    End Sub

    Public ReadOnly Property WageMonth As Date
    Public ReadOnly Property PaidGross As Decimal
    Public ReadOnly Property DueGross As Decimal
    Public ReadOnly Property GrossDifference As Decimal
    Public ReadOnly Property EpfWages As Decimal
    Public ReadOnly Property EpsWages As Decimal
    Public ReadOnly Property EdliWages As Decimal
    Public ReadOnly Property EmployeeEpf As Decimal
    Public ReadOnly Property EmployerEpf As Decimal
    Public ReadOnly Property EmployerEps As Decimal
    Public ReadOnly Property Edli As Decimal
    Public ReadOnly Property Admin As Decimal
    Public ReadOnly Property EsiEmployee As Decimal
    Public ReadOnly Property EsiEmployer As Decimal
    ''' <summary>The difference on each salary head. Heads plus OvertimeDifference add up to GrossDifference. Empty for a claim saved before heads were kept.</summary>
    Public ReadOnly Property Heads As IReadOnlyList(Of ArrearsHeadAmount)
    Public ReadOnly Property OvertimeDifference As Decimal
    Public ReadOnly Property EsiWages As Decimal

    ''' <summary>Builds the record from the calculator's answer for one month.</summary>
    Public Shared Function From(input As ArrearsMonthInput, result As ArrearsMonthResult) As ArrearsMonthRecord
        If input Is Nothing Then Throw New ArgumentNullException(NameOf(input))
        If result Is Nothing Then Throw New ArgumentNullException(NameOf(result))
        Dim heads As New List(Of ArrearsHeadAmount)()
        For Each line As EarnedLine In result.Lines
            Dim id As Integer
            If input.ComponentIds.TryGetValue(line.Code, id) Then heads.Add(New ArrearsHeadAmount(id, line.Amount))
        Next
        Return New ArrearsMonthRecord(result.WageMonth, input.Paid.GrossEarnings, input.Due.GrossEarnings, result.GrossDifference, result.EpfWages, result.EpsWages,
                                      result.EdliWages, result.EmployeeEpf, result.EmployerEpf, result.EmployerEps, result.Edli, result.Admin, result.EsiEmployee, result.EsiEmployer,
                                      heads, result.OvertimeDifference, result.EsiWages)
    End Function
End Class
