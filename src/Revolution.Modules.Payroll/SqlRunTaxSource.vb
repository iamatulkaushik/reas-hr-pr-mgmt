Imports System.Data
Imports System.Text.Json
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>
''' Tax facts for a whole run: one call to pay.usp_RunTax_Facts for the months, tax deducted and dates of birth, then each employee's declaration through the TDS repository
''' (one set of reads per employee with a declaration; no declaration gives Nothing). Tenant context comes from IConnectionFactory (ADR 0001, ADR 0008).
''' </summary>
Public NotInheritable Class SqlRunTaxSource
    Implements IRunTaxSource

    Private ReadOnly _factory As IConnectionFactory
    Private ReadOnly _tds As ITdsRepository

    Public Sub New(factory As IConnectionFactory, tds As ITdsRepository)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        If tds Is Nothing Then Throw New ArgumentNullException(NameOf(tds))
        _factory = factory
        _tds = tds
    End Sub

    Public Function Load(runId As Integer, financialYear As Integer, employeeIds As IReadOnlyList(Of Integer)) As IReadOnlyDictionary(Of Integer, EmployeeTaxFacts) Implements IRunTaxSource.Load
        Dim months As New Dictionary(Of Integer, List(Of TdsMonthFacts))()
        Dim deducted As New Dictionary(Of Integer, Decimal)()
        Dim births As New Dictionary(Of Integer, Date)()
        ReadFacts(runId, financialYear, employeeIds, months, deducted, births)
        Dim result As New Dictionary(Of Integer, EmployeeTaxFacts)()
        For Each employeeId As Integer In employeeIds
            Dim birth As Date
            If Not births.TryGetValue(employeeId, birth) Then Continue For
            Dim list As List(Of TdsMonthFacts) = Nothing
            If Not months.TryGetValue(employeeId, list) Then list = New List(Of TdsMonthFacts)()
            Dim tax As Decimal = 0D
            deducted.TryGetValue(employeeId, tax)
            result(employeeId) = New EmployeeTaxFacts(employeeId, birth, list, tax, ReadDeclaration(employeeId, financialYear))
        Next
        Return result
    End Function

    Private Sub ReadFacts(runId As Integer, financialYear As Integer, employeeIds As IReadOnlyList(Of Integer), months As Dictionary(Of Integer, List(Of TdsMonthFacts)),
                          deducted As Dictionary(Of Integer, Decimal), births As Dictionary(Of Integer, Date))
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_RunTax_Facts")
                cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                cmd.Parameters.Add("@FinancialYear", SqlDbType.Int).Value = financialYear
                cmd.Parameters.Add("@EmployeeIdsJson", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(employeeIds)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim employeeId As Integer = reader.GetInt32(reader.GetOrdinal("EmployeeID"))
                        If Not months.ContainsKey(employeeId) Then months(employeeId) = New List(Of TdsMonthFacts)()
                        months(employeeId).Add(New TdsMonthFacts(reader.GetInt32(reader.GetOrdinal("MonthKey")), reader.GetDecimal(reader.GetOrdinal("BasicPlusDa")),
                                                                 reader.GetDecimal(reader.GetOrdinal("HraReceived")), reader.GetDecimal(reader.GetOrdinal("TaxableSalary"))))
                        deducted(employeeId) = deducted.GetValueOrDefault(employeeId) + reader.GetDecimal(reader.GetOrdinal("TaxDeducted"))
                    End While
                    If reader.NextResult() Then
                        While reader.Read()
                            births(reader.GetInt32(reader.GetOrdinal("EmployeeID"))) = reader.GetDateTime(reader.GetOrdinal("DateOfBirth"))
                        End While
                    End If
                End Using
            End Using
        End Using
    End Sub

    Private Function ReadDeclaration(employeeId As Integer, financialYear As Integer) As TdsDeclarationBundle
        Dim header As TdsDeclarationHeader = _tds.GetDeclaration(employeeId, financialYear)
        If header Is Nothing Then Return Nothing
        Return New TdsDeclarationBundle(header, _tds.ListItems(header.DeclarationId), _tds.ListRents(header.DeclarationId), _tds.GetPrevEmployer(header.DeclarationId))
    End Function
End Class
