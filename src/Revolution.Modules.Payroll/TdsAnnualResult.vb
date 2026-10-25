''' <summary>The yearly tax on salary for one employee, step by step, in whole rupees (income lines are not rounded).</summary>
Public NotInheritable Class TdsAnnualResult
    Public Sub New(grossIncome As Decimal, standardDeduction As Decimal, deductions As Decimal, taxableIncome As Decimal,
                   slabTax As Decimal, rebate As Decimal, surcharge As Decimal, cess As Decimal)
        Me.GrossIncome = grossIncome
        Me.StandardDeduction = standardDeduction
        Me.Deductions = deductions
        Me.TaxableIncome = taxableIncome
        Me.SlabTax = slabTax
        Me.Rebate = rebate
        Me.Surcharge = surcharge
        Me.Cess = cess
    End Sub

    Public ReadOnly Property GrossIncome As Decimal
    Public ReadOnly Property StandardDeduction As Decimal
    ''' <summary>Declared deductions after the caps. Zero in the new regime.</summary>
    Public ReadOnly Property Deductions As Decimal
    Public ReadOnly Property TaxableIncome As Decimal
    ''' <summary>Tax on the slabs before rebate.</summary>
    Public ReadOnly Property SlabTax As Decimal
    ''' <summary>The amount taken off by section 87A (156 in the 2025 Act), including marginal relief.</summary>
    Public ReadOnly Property Rebate As Decimal
    Public ReadOnly Property Surcharge As Decimal
    Public ReadOnly Property Cess As Decimal

    Public ReadOnly Property TotalTax As Decimal
        Get
            Return SlabTax - Rebate + Surcharge + Cess
        End Get
    End Property
End Class
