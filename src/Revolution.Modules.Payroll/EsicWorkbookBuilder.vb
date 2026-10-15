''' <summary>
''' ESIC Excel workbook. Sheet 1 is the upload sheet exactly as the ESIC template; the portal reads this sheet only.
''' Sheet 2 keeps the template instructions and reason codes only. No employee data on sheet 2.
''' </summary>
Public NotInheritable Class EsicWorkbookBuilder
    Public Const UploadSheetName As String = "Sheet1"

    Private Sub New()
    End Sub

    Public Shared Function Build(rows As IReadOnlyCollection(Of EsicRow), lastDayCodes As IReadOnlyCollection(Of Integer), wageMonth As Integer) As IReadOnlyList(Of ExportSheet)
        Dim upload As IReadOnlyList(Of String()) = EsicSheetBuilder.Build(rows, lastDayCodes, wageMonth)
        Return {New ExportSheet(UploadSheetName, upload), New ExportSheet(EsicInstructions.SheetName, EsicInstructions.Table())}
    End Function
End Class
