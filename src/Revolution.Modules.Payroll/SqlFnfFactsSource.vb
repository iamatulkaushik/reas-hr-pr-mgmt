Imports System.Data
Imports System.Globalization
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Reads the facts for one separation through pay.usp_Fnf_Facts, five result sets in one call (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlFnfFactsSource
    Implements IFnfFactsSource

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function Load(separationId As Integer) As FnfSourceFacts Implements IFnfFactsSource.Load
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Fnf_Facts")
                    cmd.Parameters.Add("@SeparationID", SqlDbType.Int).Value = separationId
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        Return Read(reader)
                    End Using
                End Using
            End Using
        Catch ex As SqlException When ex.Number = 50354
            Return Nothing
        End Try
    End Function

    Private Shared Function Read(reader As SqlDataReader) As FnfSourceFacts
        If Not reader.Read() Then Return Nothing
        Dim facts As New FnfSourceFacts With {
            .EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeID")), .JoinDate = reader.GetDateTime(reader.GetOrdinal("JoinDate")),
            .DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")), .EmploymentTypeId = CInt(reader.GetByte(reader.GetOrdinal("EmploymentTypeID"))),
            .ReasonId = CInt(reader.GetByte(reader.GetOrdinal("ReasonID"))), .NoticeDate = NullableDate(reader, "NoticeDate"),
            .LastWorkingDay = reader.GetDateTime(reader.GetOrdinal("LastWorkingDay")), .NoticePeriodDays = CInt(reader.GetInt16(reader.GetOrdinal("NoticePeriodDays"))),
            .DailyWageDivisor = reader.GetDecimal(reader.GetOrdinal("DailyWageDivisor"))}
        If reader.NextResult() AndAlso reader.Read() Then
            facts.MonthlyGross = reader.GetDecimal(reader.GetOrdinal("MonthlyGross"))
            facts.MonthlyBasicPlusDa = reader.GetDecimal(reader.GetOrdinal("MonthlyBasicPlusDa"))
        End If
        If reader.NextResult() AndAlso reader.Read() Then facts.LoanOutstanding = Math.Max(0D, reader.GetDecimal(reader.GetOrdinal("LoanOutstanding")))
        Dim months As New List(Of FnfWorkedMonth)()
        If reader.NextResult() Then
            While reader.Read()
                months.Add(New FnfWorkedMonth(reader.GetInt32(reader.GetOrdinal("MonthKey")), reader.GetDecimal(reader.GetOrdinal("BasicPlusDa")), reader.GetDecimal(reader.GetOrdinal("PaidDays"))))
            End While
        End If
        facts.BonusMonths = months
        If reader.NextResult() AndAlso reader.Read() Then
            facts.FinalRunId = reader.GetInt32(reader.GetOrdinal("RunID"))
            facts.FinalMonthKey = reader.GetInt32(reader.GetOrdinal("MonthKey"))
            facts.FinalNetPay = reader.GetDecimal(reader.GetOrdinal("NetPay"))
            facts.FinalGross = reader.GetDecimal(reader.GetOrdinal("GrossEarnings"))
            facts.FinalPaidDays = reader.GetDecimal(reader.GetOrdinal("PaidDays"))
            facts.FinalTdsAmount = reader.GetDecimal(reader.GetOrdinal("TdsAmount"))
            facts.FinalBasicPlusDa = reader.GetDecimal(reader.GetOrdinal("BasicPlusDa"))
            facts.FinalHra = reader.GetDecimal(reader.GetOrdinal("Hra"))
        End If
        Return facts
    End Function

    Private Shared Function NullableDate(reader As SqlDataReader, column As String) As Date?
        Dim ordinal As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(ordinal), CType(Nothing, Date?), reader.GetDateTime(ordinal))
    End Function
End Class
