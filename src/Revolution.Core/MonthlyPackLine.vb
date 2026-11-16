''' <summary>
''' One statutory item of a company for a month (ADR 0027): what it is, where it stands, what is payable and by when. The figures come from the approved or locked payroll of the month.
''' DueDate is empty, with a DueNote ("Date not set"), when no CA-verified due-date rule covers the month.
''' </summary>
Public NotInheritable Class MonthlyPackLine
    Public Sub New(kind As ComplianceKind, title As String, stateId As Integer?, stateName As String, status As PackStatus, amountPayable As Decimal?, employeeCount As Integer,
                   dueDate As Date?, dueNote As String, reference As String, message As String, components As IReadOnlyList(Of PackAmount))
        Me.Kind = kind
        Me.Title = If(title, String.Empty)
        Me.StateId = stateId
        Me.StateName = If(stateName, String.Empty)
        Me.Status = status
        Me.AmountPayable = amountPayable
        Me.EmployeeCount = employeeCount
        Me.DueDate = dueDate
        Me.DueNote = If(dueNote, String.Empty)
        Me.Reference = If(reference, String.Empty)
        Me.Message = If(message, String.Empty)
        Me.Components = If(components, New List(Of PackAmount)())
    End Sub

    Public ReadOnly Property Kind As ComplianceKind
    Public ReadOnly Property Title As String
    ''' <summary>The state of a professional tax or labour welfare line; empty for the others.</summary>
    Public ReadOnly Property StateId As Integer?
    Public ReadOnly Property StateName As String
    Public ReadOnly Property Status As PackStatus
    Public ReadOnly Property AmountPayable As Decimal?
    Public ReadOnly Property EmployeeCount As Integer
    Public ReadOnly Property DueDate As Date?
    Public ReadOnly Property DueNote As String
    ''' <summary>The filing or payment reference the Associate entered (acknowledgement or challan number).</summary>
    Public ReadOnly Property Reference As String
    ''' <summary>Why a line is not ready, or what changed.</summary>
    Public ReadOnly Property Message As String
    Public ReadOnly Property Components As IReadOnlyList(Of PackAmount)
End Class
