Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Optional ESIC detail sheet with a totals row. NOT part of the upload workbook.
''' PENDING: decide on runtime testing whether it becomes a third sheet, a separate file, or is dropped.
''' </summary>
Public NotInheritable Class EsicDetailSheetBuilder
    Public Const SheetName As String = "Details"

    Public Shared ReadOnly Header As String() = {
        "Sr No", "Employee Code", "IP Number", "IP Name", "Gross Earnings", "Days Paid", "ESI Wage",
        "Employee Share", "Employer Share", "Total Contribution", "Reason Code", "Last Working Day"}

    Private Sub New()
    End Sub

    Public Shared Function Build(details As IReadOnlyCollection(Of EsicDetail)) As ExportSheet
        If details Is Nothing Then
            Throw New ArgumentNullException(NameOf(details))
        End If
        Dim table As New List(Of String())()
        table.Add(Header)
        Dim serial As Integer = 0
        For Each d As EsicDetail In details
            serial += 1
            Dim cells As String() = EsicSheetBuilder.Cells(d.Row)
            table.Add({serial.ToString(CultureInfo.InvariantCulture), d.EmployeeCode, cells(0), cells(1), Money(d.GrossEarnings),
                       cells(2), cells(3), Money(d.EmployeeShare), Money(d.EmployerShare),
                       Money(d.EmployeeShare + d.EmployerShare), cells(4), cells(5)})
        Next
        table.Add(TotalRow(details))
        Return New ExportSheet(SheetName, table)
    End Function

    Private Shared Function TotalRow(details As IReadOnlyCollection(Of EsicDetail)) As String()
        Dim employee As Decimal = details.Sum(Function(d) d.EmployeeShare)
        Dim employer As Decimal = details.Sum(Function(d) d.EmployerShare)
        Return {String.Empty, String.Empty, String.Empty, "Total", Money(details.Sum(Function(d) d.GrossEarnings)), String.Empty,
                Money(details.Sum(Function(d) d.Row.TotalWages)), Money(employee), Money(employer), Money(employee + employer),
                String.Empty, String.Empty}
    End Function

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("0.00", CultureInfo.InvariantCulture)
    End Function
End Class
