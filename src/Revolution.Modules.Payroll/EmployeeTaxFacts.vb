''' <summary>
''' What the income tax projection needs about one employee for one run, read once for the whole run. LockedMonths holds one entry per month (yyyymm) of the financial year from
''' locked or paid runs of the company, including arrears runs, never from the run being processed. TaxDeductedBefore is the tax deducted in those same runs. Declaration is Nothing
''' when the employee has none for the year, and the projection then uses the new regime with no claims.
''' </summary>
Public NotInheritable Class EmployeeTaxFacts
    Public Sub New(employeeId As Integer, dateOfBirth As Date, lockedMonths As IReadOnlyList(Of TdsMonthFacts), taxDeductedBefore As Decimal, declaration As TdsDeclarationBundle)
        If lockedMonths Is Nothing Then Throw New ArgumentNullException(NameOf(lockedMonths))
        If taxDeductedBefore < 0D Then Throw New ArgumentOutOfRangeException(NameOf(taxDeductedBefore), "Tax deducted cannot be negative.")
        Me.EmployeeId = employeeId
        Me.DateOfBirth = dateOfBirth
        Me.LockedMonths = lockedMonths
        Me.TaxDeductedBefore = taxDeductedBefore
        Me.Declaration = declaration
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property DateOfBirth As Date
    Public ReadOnly Property LockedMonths As IReadOnlyList(Of TdsMonthFacts)
    Public ReadOnly Property TaxDeductedBefore As Decimal
    Public ReadOnly Property Declaration As TdsDeclarationBundle
End Class
