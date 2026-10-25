''' <summary>Income and tax from the previous employer in the same financial year (Form 122 from 2026-27, was 12B). Perquisites are optional.</summary>
Public NotInheritable Class TdsPrevEmployer
    Public Sub New(employerName As String, employerTan As String, salaryIncome As Decimal, perquisites As Decimal, providentFund As Decimal,
                   professionalTax As Decimal, taxDeducted As Decimal)
        Me.EmployerName = If(employerName, String.Empty).Trim()
        Me.EmployerTan = If(employerTan, String.Empty).Trim().ToUpperInvariant()
        Me.SalaryIncome = salaryIncome
        Me.Perquisites = perquisites
        Me.ProvidentFund = providentFund
        Me.ProfessionalTax = professionalTax
        Me.TaxDeducted = taxDeducted
    End Sub

    Public ReadOnly Property EmployerName As String
    Public ReadOnly Property EmployerTan As String
    Public ReadOnly Property SalaryIncome As Decimal
    Public ReadOnly Property Perquisites As Decimal
    Public ReadOnly Property ProvidentFund As Decimal
    Public ReadOnly Property ProfessionalTax As Decimal
    Public ReadOnly Property TaxDeducted As Decimal
End Class
