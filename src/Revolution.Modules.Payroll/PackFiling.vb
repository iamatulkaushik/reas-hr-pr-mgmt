Imports Revolution.Core

''' <summary>One recorded line of pay.StatutoryFiling: what was generated for a company, month and item, and whether it was filed or paid.</summary>
Public NotInheritable Class PackFiling
    Public Sub New(kind As ComplianceKind, stateId As Integer?, status As PackStatus, amountPayable As Decimal, employeeCount As Integer,
                   filedOn As Date?, paidOn As Date?, referenceNo As String)
        Me.Kind = kind
        Me.StateId = stateId
        Me.Status = status
        Me.AmountPayable = amountPayable
        Me.EmployeeCount = employeeCount
        Me.FiledOn = filedOn
        Me.PaidOn = paidOn
        Me.ReferenceNo = If(referenceNo, String.Empty)
    End Sub

    Public ReadOnly Property Kind As ComplianceKind
    Public ReadOnly Property StateId As Integer?
    Public ReadOnly Property Status As PackStatus
    ''' <summary>The amount when the line was generated; a later difference makes the line Changed.</summary>
    Public ReadOnly Property AmountPayable As Decimal
    Public ReadOnly Property EmployeeCount As Integer
    Public ReadOnly Property FiledOn As Date?
    Public ReadOnly Property PaidOn As Date?
    Public ReadOnly Property ReferenceNo As String
End Class
