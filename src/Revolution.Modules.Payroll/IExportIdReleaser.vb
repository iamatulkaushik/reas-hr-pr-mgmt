Imports Revolution.Core

''' <summary>
''' Releases the plain ids for the employees of one export. The real one calls the Employees module, which checks the
''' permission, writes the batch log first and then decrypts. A failure means no id was released.
''' </summary>
Public Interface IExportIdReleaser
    Function Release(kind As PayrollFileKind, employeeIds As IReadOnlyCollection(Of Integer)) As OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds))
End Interface
