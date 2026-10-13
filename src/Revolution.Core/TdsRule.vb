''' <summary>
''' Everything the TDS calculation needs for one financial year, regime and age band: standard deduction, tax slabs,
''' rebate (section 87A) limit and maximum, surcharge bands and cess. No surcharge bands means no surcharge applies.
''' </summary>
Public NotInheritable Class TdsRule
    Public Sub New(financialYear As Integer, regime As TdsRegime, ageBand As TdsAgeBand, standardDeduction As Decimal,
                   rebateIncomeLimit As Decimal, rebateMaximum As Decimal, cessPct As Decimal, verified As Boolean,
                   sourceRef As String, slabs As IReadOnlyList(Of TdsSlab), surcharges As IReadOnlyList(Of TdsSurchargeBand))
        Me.FinancialYear = financialYear
        Me.Regime = regime
        Me.AgeBand = ageBand
        Me.StandardDeduction = standardDeduction
        Me.RebateIncomeLimit = rebateIncomeLimit
        Me.RebateMaximum = rebateMaximum
        Me.CessPct = cessPct
        Me.Verified = verified
        Me.SourceRef = If(sourceRef, String.Empty)
        Me.Slabs = If(slabs, New List(Of TdsSlab)())
        Me.Surcharges = If(surcharges, New List(Of TdsSurchargeBand)())
    End Sub

    ''' <summary>The year the financial year starts in: 2026 means 1 April 2026 to 31 March 2027.</summary>
    Public ReadOnly Property FinancialYear As Integer
    Public ReadOnly Property Regime As TdsRegime
    Public ReadOnly Property AgeBand As TdsAgeBand
    Public ReadOnly Property StandardDeduction As Decimal
    Public ReadOnly Property RebateIncomeLimit As Decimal
    Public ReadOnly Property RebateMaximum As Decimal
    Public ReadOnly Property CessPct As Decimal
    ''' <summary>The CA's sign-off covers the parameters, the slabs and the surcharge bands together.</summary>
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property SourceRef As String
    Public ReadOnly Property Slabs As IReadOnlyList(Of TdsSlab)
    Public ReadOnly Property Surcharges As IReadOnlyList(Of TdsSurchargeBand)
End Class
