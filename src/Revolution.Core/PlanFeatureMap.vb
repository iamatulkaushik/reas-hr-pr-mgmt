''' <summary>Lowest plan that includes a feature (ADR 0003 draft split). Higher plans include everything below.</summary>
Public NotInheritable Class PlanFeatureMap
    Private Sub New()
    End Sub

    Public Shared Function MinimumTier(feature As PlanFeature) As PlanTier
        Select Case feature
            Case PlanFeature.Employees, PlanFeature.Attendance, PlanFeature.BasicPayroll, PlanFeature.Payslips
                Return PlanTier.Copper
            Case PlanFeature.EpfEsi, PlanFeature.ProfessionalTax, PlanFeature.Loans, PlanFeature.BankFiles
                Return PlanTier.Silver
            Case PlanFeature.Tds, PlanFeature.Form16And24Q, PlanFeature.Arrears, PlanFeature.FullAndFinal,
                 PlanFeature.Biometric, PlanFeature.ShiftOtLop
                Return PlanTier.Gold
            Case PlanFeature.DualApproval, PlanFeature.FullAuditTrail, PlanFeature.AssetsAndExpenses,
                 PlanFeature.MultiCompany
                Return PlanTier.Platinum
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(feature), feature, "Unknown plan feature.")
        End Select
    End Function

    Public Shared Function IsIncluded(plan As PlanTier, feature As PlanFeature) As Boolean
        Return plan >= MinimumTier(feature)
    End Function
End Class
