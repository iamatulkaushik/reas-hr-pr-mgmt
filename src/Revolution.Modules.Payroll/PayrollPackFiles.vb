Imports Revolution.Core

''' <summary>IPackFiles over PayrollFileService (it checks permission, decrypts ids only for an approved run and logs each file first, ADR 0019) and the EPF rate.</summary>
Public NotInheritable Class PayrollPackFiles
    Implements IPackFiles

    Private ReadOnly _files As PayrollFileService
    Private ReadOnly _rates As IStatutoryRateService

    Public Sub New(files As PayrollFileService, rates As IStatutoryRateService)
        If files Is Nothing Then Throw New ArgumentNullException(NameOf(files))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        _files = files
        _rates = rates
    End Sub

    Public Function Ecr(runId As Integer, periodTo As Date) As OperationResult(Of FileOutput(Of String)) Implements IPackFiles.Ecr
        Dim rate = _rates.GetEpfRate(periodTo)
        If Not rate.Succeeded Then Return OperationResult(Of FileOutput(Of String)).Fail(rate.Message)
        Return _files.BuildEcr(runId, rate.Value.WageCeiling)
    End Function

    Public Function Esic(runId As Integer) As OperationResult(Of FileOutput(Of IReadOnlyList(Of ExportSheet))) Implements IPackFiles.Esic
        Return _files.BuildEsic(runId)
    End Function
End Class
