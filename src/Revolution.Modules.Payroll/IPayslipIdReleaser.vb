Imports Revolution.Core

''' <summary>
''' Releases the plain UAN, ESI IP number and account number of the employees on a payslip print. The real one calls the Employees module, which checks Export on
''' Payroll, writes the batch log first and then decrypts (ADR 0019). A failure means no id was released.
''' </summary>
Public Interface IPayslipIdReleaser
    Function ReleaseForPayslips(employeeIds As IReadOnlyCollection(Of Integer)) As OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds))
End Interface
