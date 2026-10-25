Imports Revolution.Core

''' <summary>
''' Yearly and monthly tax on salary. Slabs, standard deduction, rebate, surcharge, cess and deduction caps all come in as rows
''' (TdsRule, TdsDeductionLimit); nothing is typed here. Money is Decimal, whole rupees at the end, midpoint away from zero.
''' New regime: no declared deductions, and marginal relief on the rebate just above its income limit (the old regime has none).
''' Not done here, pending the CA (CR 0018): rounding of income and tax to 10 rupees.
''' </summary>
Public NotInheritable Class TdsCalculator
    Private Sub New()
    End Sub

    ''' <param name="grossIncome">Projected salary for the year plus previous employer income, less exempt allowances and HRA exemption.</param>
    Public Shared Function Annual(grossIncome As Decimal, claims As IEnumerable(Of TdsDeductionClaim),
                                  rule As TdsRule, limits As IEnumerable(Of TdsDeductionLimit)) As TdsAnnualResult
        If rule Is Nothing Then
            Throw New ArgumentNullException(NameOf(rule))
        End If
        If grossIncome < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(grossIncome), "Income cannot be negative.")
        End If
        Dim deductions As Decimal = If(rule.Regime = TdsRegime.Old, TdsDeductionCapper.Total(claims, limits), 0D)
        Dim taxable As Decimal = Math.Max(0D, grossIncome - rule.StandardDeduction - deductions)
        Dim slabTax As Decimal = TdsSlabTax.Compute(taxable, rule)
        Dim rebate As Decimal = RebateFor(taxable, slabTax, rule)
        Dim surcharge As Decimal = TdsSurchargeCalculator.Compute(taxable, slabTax - rebate, rule)
        Dim cess As Decimal = Rupee.Percent(slabTax - rebate + surcharge, rule.CessPct)
        Return New TdsAnnualResult(grossIncome, rule.StandardDeduction, deductions, taxable,
                                   WholeRupee.Round(slabTax), WholeRupee.Round(rebate), WholeRupee.Round(surcharge), WholeRupee.Round(cess))
    End Function

    ''' <summary>Tax for this month: what is left of the year's tax, spread equally over the remaining months, nearest rupee. The last month takes the balance.</summary>
    Public Shared Function Monthly(annualTax As Decimal, alreadyDeducted As Decimal, remainingMonths As Integer) As Decimal
        If remainingMonths < 1 Then
            Throw New ArgumentOutOfRangeException(NameOf(remainingMonths), "At least one month must remain.")
        End If
        If annualTax < 0D OrElse alreadyDeducted < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(annualTax), "Tax cannot be negative.")
        End If
        Dim left As Decimal = annualTax - alreadyDeducted
        If left <= 0D Then
            Return 0D
        End If
        Return Rupee.Round(left / remainingMonths, RateRounding.Nearest)
    End Function

    Private Shared Function RebateFor(taxable As Decimal, slabTax As Decimal, rule As TdsRule) As Decimal
        If rule.RebateMaximum <= 0D OrElse slabTax <= 0D Then
            Return 0D
        End If
        If taxable <= rule.RebateIncomeLimit Then
            Return Math.Min(slabTax, rule.RebateMaximum)
        End If
        If rule.Regime = TdsRegime.New AndAlso slabTax > taxable - rule.RebateIncomeLimit Then
            Return slabTax - (taxable - rule.RebateIncomeLimit)
        End If
        Return 0D
    End Function
End Class
