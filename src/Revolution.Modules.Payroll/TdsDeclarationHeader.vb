Imports Revolution.Core

''' <summary>One employee's declaration for one financial year (the start year: 2026 is 2026-27).</summary>
Public NotInheritable Class TdsDeclarationHeader
    Public Sub New(declarationId As Integer, employeeId As Integer, financialYear As Integer, regime As TdsRegime, status As TdsDeclarationStatus)
        Me.DeclarationId = declarationId
        Me.EmployeeId = employeeId
        Me.FinancialYear = financialYear
        Me.Regime = regime
        Me.Status = status
    End Sub

    Public ReadOnly Property DeclarationId As Integer
    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property FinancialYear As Integer
    Public ReadOnly Property Regime As TdsRegime
    Public ReadOnly Property Status As TdsDeclarationStatus
End Class
