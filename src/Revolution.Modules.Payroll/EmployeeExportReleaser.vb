Imports Revolution.Core

''' <summary>
''' Adapter from the Payroll file source to the Employees module (ADR 0019). The Employees module checks the Export permission,
''' writes the batch log first, and decrypts only the ids the kind of file needs. Full names are used because Employees also
''' has a PaymentMode type.
''' </summary>
Public NotInheritable Class EmployeeExportReleaser
    Implements IExportIdReleaser
    Implements IPayslipIdReleaser

    Private ReadOnly _export As Revolution.Modules.Employees.EmployeeExportService

    Public Sub New(export As Revolution.Modules.Employees.EmployeeExportService)
        If export Is Nothing Then Throw New ArgumentNullException(NameOf(export))
        _export = export
    End Sub

    Public Function Release(kind As PayrollFileKind, employeeIds As IReadOnlyCollection(Of Integer)) As OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)) Implements IExportIdReleaser.Release
        Dim revealed As OperationResult(Of IReadOnlyDictionary(Of Integer, Revolution.Modules.Employees.ExportIds)) = _export.RevealForExport(ToExportKind(kind), employeeIds)
        If Not revealed.Succeeded Then Return OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)).Fail(revealed.Message)
        Dim released As New Dictionary(Of Integer, ReleasedIds)()
        For Each pair As KeyValuePair(Of Integer, Revolution.Modules.Employees.ExportIds) In revealed.Value
            released(pair.Key) = New ReleasedIds With {.Uan = pair.Value.Uan, .EsiIp = pair.Value.EsiIp, .AccountNumber = pair.Value.AccountNumber}
        Next
        Return OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)).Ok(released)
    End Function

    Public Function ReleaseForPayslips(employeeIds As IReadOnlyCollection(Of Integer)) As OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)) Implements IPayslipIdReleaser.ReleaseForPayslips
        Dim revealed As OperationResult(Of IReadOnlyDictionary(Of Integer, Revolution.Modules.Employees.ExportIds)) = _export.RevealForExport(Revolution.Modules.Employees.ExportKind.Payslip, employeeIds)
        If Not revealed.Succeeded Then Return OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)).Fail(revealed.Message)
        Dim released As New Dictionary(Of Integer, ReleasedIds)()
        For Each pair As KeyValuePair(Of Integer, Revolution.Modules.Employees.ExportIds) In revealed.Value
            released(pair.Key) = New ReleasedIds With {.Uan = pair.Value.Uan, .EsiIp = pair.Value.EsiIp, .AccountNumber = pair.Value.AccountNumber}
        Next
        Return OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)).Ok(released)
    End Function

    Private Shared Function ToExportKind(kind As PayrollFileKind) As Revolution.Modules.Employees.ExportKind
        Select Case kind
            Case PayrollFileKind.Ecr
                Return Revolution.Modules.Employees.ExportKind.Ecr
            Case PayrollFileKind.Esic
                Return Revolution.Modules.Employees.ExportKind.Esic
            Case Else
                Return Revolution.Modules.Employees.ExportKind.BankFile
        End Select
    End Function
End Class
