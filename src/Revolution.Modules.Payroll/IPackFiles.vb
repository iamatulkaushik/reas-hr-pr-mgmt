Imports Revolution.Core

''' <summary>The two statutory files the Monthly Pack makes, ECR and the ESIC sheet. Adapter over PayrollFileService; it also finds the EPF wage ceiling for the month.</summary>
Public Interface IPackFiles
    Function Ecr(runId As Integer, periodTo As Date) As OperationResult(Of FileOutput(Of String))

    Function Esic(runId As Integer) As OperationResult(Of FileOutput(Of IReadOnlyList(Of ExportSheet)))
End Interface
