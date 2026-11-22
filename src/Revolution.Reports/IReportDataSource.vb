''' <summary>Rows for the report screens. The screens never touch the database: this is the only way in (UI -> Services -> Data).</summary>
Public Interface IReportDataSource
    Function ListRuns() As IReadOnlyList(Of ReportRunInfo)
    Function GetRows(entry As ReportEntry, choice As ReportChoice) As IReadOnlyList(Of IReadOnlyDictionary(Of String, Object))
End Interface
