''' <summary>
''' Loads a run for file export. The implementation reads the run lines, decrypts UAN, ESI IP number and bank account
''' (each with its reveal log row, ADR 0011) and fills PayrollFileEmployee. This module never sees keys or the database.
''' Returns Nothing when the run is not found for the current company.
''' </summary>
Public Interface IPayrollFileSource
    Function Load(runId As Integer) As PayrollFileSnapshot
End Interface
