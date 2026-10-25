''' <summary>
''' What the projection needs for one employee in one run. Months holds all twelve months of the financial year, April to March: earned months from locked runs,
''' this month from the run being worked, later months at the current salary structure. TaxDeductedBefore is the TDS already taken by this employer this
''' year, not counting this month. Declaration is Nothing when the employee has none (new regime, no deductions).
''' </summary>
Public NotInheritable Class TdsProjectionInput
    Public Sub New(employeeId As Integer, financialYear As Integer, dateOfBirth As Date, runMonthKey As Integer, months As IReadOnlyList(Of TdsMonthFacts),
                   taxDeductedBefore As Decimal, declaration As TdsDeclarationBundle)
        Me.EmployeeId = employeeId
        Me.FinancialYear = financialYear
        Me.DateOfBirth = dateOfBirth
        Me.RunMonthKey = runMonthKey
        Me.Months = months
        Me.TaxDeductedBefore = taxDeductedBefore
        Me.Declaration = declaration
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property FinancialYear As Integer
    Public ReadOnly Property DateOfBirth As Date
    Public ReadOnly Property RunMonthKey As Integer
    Public ReadOnly Property Months As IReadOnlyList(Of TdsMonthFacts)
    Public ReadOnly Property TaxDeductedBefore As Decimal
    Public ReadOnly Property Declaration As TdsDeclarationBundle
End Class
