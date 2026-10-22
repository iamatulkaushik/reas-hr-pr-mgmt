''' <summary>Employee storage through mst.* stored procedures (ADR 0008). Never receives or returns a clear id.</summary>
Public Interface IEmployeeRepository
    ''' <summary>Creates the employee with the next code for the company. Ids are saved separately.</summary>
    Function Create(draft As EmployeeDraft, placement As EmployeePlacement, userId As Integer?) As NewEmployee

    ''' <summary>Updates the employee. A stale rowVersion raises EmployeeRuleException. Ids are not touched.</summary>
    Sub Update(employeeId As Integer, rowVersion As Byte(), draft As EmployeeDraft, placement As EmployeePlacement, userId As Integer?)

    ''' <summary>Nothing when the employee is not in this company.</summary>
    Function GetById(employeeId As Integer) As EmployeeDetail

    Function List(search As String, factoryId As Integer?, activeOnly As Boolean) As IReadOnlyList(Of EmployeeSummary)

    Sub RecordExit(employeeId As Integer, exitDate As Date, status As EmployeeStatus, userId As Integer?)

    Sub Reactivate(employeeId As Integer, userId As Integer?)

    ''' <summary>Nothing when the employee has no statutory row yet.</summary>
    Function GetIdentifierLast4(employeeId As Integer) As IdentifierLast4

    ''' <summary>Nothing when the employee has no statutory row yet. Callers must hold the Reveal permission or be re-encrypting.</summary>
    Function GetEncryptedIdentifiers(employeeId As Integer) As EncryptedIdentifiers

    Sub SetIdentifiers(employeeId As Integer, write As IdentifierWrite, userId As Integer?)

    ''' <summary>Writes aud.RevealLog. Fields are names only, never values.</summary>
    Sub LogReveal(userId As Integer, employeeId As Integer, fields As String)

    ''' <summary>One reveal log row per employee for one export, all or none (aud.usp_Reveal_InsertBatch). Field names only.</summary>
    Sub LogRevealBatch(userId As Integer, reason As String, fields As String, employeeIds As IReadOnlyCollection(Of Integer))
End Interface
