Imports Revolution.Core

''' <summary>
''' Loads a run for file export. The implementation reads the run lines, releases UAN, ESI IP number and bank account for the
''' kind of file only (each batch logged before anything is decrypted, ADR 0019) and fills PayrollFileEmployee.
''' This module never sees keys. Fails with a plain message when the run is not found for the current company,
''' is not yet approved, or the ids cannot be released. No id is released for a run that is not approved.
''' </summary>
Public Interface IPayrollFileSource
    Function Load(runId As Integer, kind As PayrollFileKind) As OperationResult(Of PayrollFileSnapshot)
End Interface
