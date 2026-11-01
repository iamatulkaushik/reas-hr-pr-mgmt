Imports System.Globalization
Imports System.Text

''' <summary>Builds the arrear ECR text for one wage month: one line per member, eight fields, #~# between fields, no header. Rows with nothing to report are left out. A negative amount is refused: EPFO takes arrear amounts as positive.</summary>
Public NotInheritable Class ArrearsEcrWriter
    Private Sub New()
    End Sub

    Public Shared Function Build(rows As IReadOnlyCollection(Of ArrearsEcrRow)) As String
        If rows Is Nothing Then Throw New ArgumentNullException(NameOf(rows))
        Dim text As New StringBuilder()
        For Each row As ArrearsEcrRow In rows
            Dim problem As String = Check(row)
            If problem.Length > 0 Then Throw New InvalidOperationException("Arrear ECR is not valid: " & problem)
            If IsEmpty(row) Then Continue For
            text.Append(Line(row)).Append(vbCrLf)
        Next
        Return text.ToString()
    End Function

    Public Shared Function Line(row As ArrearsEcrRow) As String
        Dim fields As String() = {
            row.Uan,
            EcrNameCleaner.Clean(row.Name),
            Whole(row.EpfWages),
            Whole(row.EpsWages),
            Whole(row.EdliWages),
            Whole(row.EmployeeEpf),
            Whole(row.EpsShare),
            Whole(row.EmployerEpf)
        }
        Return String.Join(EcrFileWriter.Separator, fields)
    End Function

    Private Shared Function Check(row As ArrearsEcrRow) As String
        If String.IsNullOrWhiteSpace(row.Uan) OrElse row.Uan.Length <> 12 OrElse Not row.Uan.All(Function(c As Char) Char.IsDigit(c)) Then
            Return "UAN must be 12 digits."
        End If
        Dim amounts As Decimal() = {row.EpfWages, row.EpsWages, row.EdliWages, row.EmployeeEpf, row.EpsShare, row.EmployerEpf}
        If amounts.Any(Function(a As Decimal) a < 0D) Then
            Return "UAN " & row.Uan & " has a negative arrear amount."
        End If
        Return String.Empty
    End Function

    Private Shared Function IsEmpty(row As ArrearsEcrRow) As Boolean
        Return row.EpfWages = 0D AndAlso row.EpsWages = 0D AndAlso row.EdliWages = 0D AndAlso row.EmployeeEpf = 0D AndAlso row.EpsShare = 0D AndAlso row.EmployerEpf = 0D
    End Function

    Private Shared Function Whole(amount As Decimal) As String
        Return Decimal.Truncate(amount).ToString(CultureInfo.InvariantCulture)
    End Function
End Class
