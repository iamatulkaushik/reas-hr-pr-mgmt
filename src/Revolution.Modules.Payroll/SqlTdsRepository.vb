Imports System.Data
Imports System.Globalization
Imports System.Text.Json
Imports Microsoft.Data.SqlClient
Imports Revolution.Core
Imports Revolution.Data

''' <summary>TDS declaration storage through pay.usp_TdsDeclaration_*, usp_TdsDeclarationItem_*, usp_TdsHraRent_* and usp_TdsPrevEmployer_* (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlTdsRepository
    Implements ITdsRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function GetDeclaration(employeeId As Integer, financialYear As Integer) As TdsDeclarationHeader Implements ITdsRepository.GetDeclaration
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_TdsDeclaration_Get")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                cmd.Parameters.Add("@FinancialYear", SqlDbType.SmallInt).Value = CShort(financialYear)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If Not reader.Read() Then Return Nothing
                    Return New TdsDeclarationHeader(reader.GetInt32(reader.GetOrdinal("DeclarationID")), reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                                    CInt(reader.GetInt16(reader.GetOrdinal("FinancialYear"))),
                                                    CType(CInt(reader.GetByte(reader.GetOrdinal("RegimeID"))), TdsRegime),
                                                    CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), TdsDeclarationStatus))
                End Using
            End Using
        End Using
    End Function

    Public Function SaveDeclaration(employeeId As Integer, financialYear As Integer, regime As TdsRegime, userId As Integer?) As Integer Implements ITdsRepository.SaveDeclaration
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_TdsDeclaration_Save")
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@FinancialYear", SqlDbType.SmallInt).Value = CShort(financialYear)
                    cmd.Parameters.Add("@RegimeID", SqlDbType.TinyInt).Value = CByte(regime)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    Return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Function

    Public Sub SetStatus(declarationId As Integer, status As TdsDeclarationStatus, userId As Integer?) Implements ITdsRepository.SetStatus
        Execute("pay.usp_TdsDeclaration_SetStatus",
                Sub(cmd)
                    cmd.Parameters.Add("@DeclarationID", SqlDbType.Int).Value = declarationId
                    cmd.Parameters.Add("@StatusID", SqlDbType.TinyInt).Value = CByte(status)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Function ListItems(declarationId As Integer) As IReadOnlyList(Of TdsItem) Implements ITdsRepository.ListItems
        Dim items As New List(Of TdsItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_TdsDeclarationItem_List")
                cmd.Parameters.Add("@DeclarationID", SqlDbType.Int).Value = declarationId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New TdsItem(reader.GetString(reader.GetOrdinal("SectionCode")), reader.GetString(reader.GetOrdinal("SubLimitKey")),
                                              ReadText(reader, "Description"), reader.GetDecimal(reader.GetOrdinal("DeclaredAmount")),
                                              ReadText(reader, "ProofRef"), ReadAmount(reader, "VerifiedAmount")))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Sub ReplaceItems(declarationId As Integer, items As IReadOnlyList(Of TdsItem), userId As Integer?) Implements ITdsRepository.ReplaceItems
        Dim numbered As New List(Of Object)()
        For index As Integer = 0 To items.Count - 1
            Dim item As TdsItem = items(index)
            numbered.Add(New With {.No = index + 1, .Section = item.SectionCode, .SubKey = item.SubLimitKey, .Description = item.Description,
                                   .Declared = item.DeclaredAmount, .Proof = item.ProofRef, .Verified = item.VerifiedAmount})
        Next
        Dim json As String = JsonSerializer.Serialize(numbered)
        Execute("pay.usp_TdsDeclarationItem_Replace",
                Sub(cmd)
                    cmd.Parameters.Add("@DeclarationID", SqlDbType.Int).Value = declarationId
                    cmd.Parameters.Add("@ItemsJson", SqlDbType.NVarChar, -1).Value = json
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Function ListRents(declarationId As Integer) As IReadOnlyList(Of TdsRentView) Implements ITdsRepository.ListRents
        Dim rents As New List(Of TdsRentView)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_TdsHraRent_List")
                cmd.Parameters.Add("@DeclarationID", SqlDbType.Int).Value = declarationId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        rents.Add(New TdsRentView(CInt(reader.GetInt16(reader.GetOrdinal("RentNo"))), reader.GetInt32(reader.GetOrdinal("FromMonthKey")),
                                                  reader.GetInt32(reader.GetOrdinal("ToMonthKey")), reader.GetDecimal(reader.GetOrdinal("MonthlyRent")),
                                                  reader.GetString(reader.GetOrdinal("City")), reader.GetString(reader.GetOrdinal("LandlordName")),
                                                  reader.GetString(reader.GetOrdinal("LandlordRelationship")), ReadText(reader, "LandlordAddress"),
                                                  ReadText(reader, "LandlordPanLast4")))
                    End While
                End Using
            End Using
        End Using
        Return rents
    End Function

    Public Sub ReplaceRents(declarationId As Integer, rents As IReadOnlyList(Of TdsRentWrite), userId As Integer?) Implements ITdsRepository.ReplaceRents
        Dim rows As New List(Of Object)()
        For Each write As TdsRentWrite In rents
            rows.Add(New With {.No = write.RentNo, .From = write.Rent.FromMonthKey, .To = write.Rent.ToMonthKey, .Rent = write.Rent.MonthlyRent,
                               .City = write.Rent.City, .Landlord = write.Rent.LandlordName, .Relationship = write.Rent.LandlordRelationship,
                               .Address = write.Rent.LandlordAddress,
                               .PanEnc = If(write.PanEnc Is Nothing, Nothing, Convert.ToBase64String(write.PanEnc)),
                               .PanLast4 = write.PanLast4, .KeyVersion = CInt(write.KeyVersion)})
        Next
        Dim json As String = JsonSerializer.Serialize(rows)
        Execute("pay.usp_TdsHraRent_Replace",
                Sub(cmd)
                    cmd.Parameters.Add("@DeclarationID", SqlDbType.Int).Value = declarationId
                    cmd.Parameters.Add("@RentsJson", SqlDbType.NVarChar, -1).Value = json
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Function GetPrevEmployer(declarationId As Integer) As TdsPrevEmployer Implements ITdsRepository.GetPrevEmployer
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_TdsPrevEmployer_Get")
                cmd.Parameters.Add("@DeclarationID", SqlDbType.Int).Value = declarationId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If Not reader.Read() Then Return Nothing
                    Return New TdsPrevEmployer(reader.GetString(reader.GetOrdinal("EmployerName")), ReadText(reader, "EmployerTan"),
                                               reader.GetDecimal(reader.GetOrdinal("SalaryIncome")), reader.GetDecimal(reader.GetOrdinal("Perquisites")),
                                               reader.GetDecimal(reader.GetOrdinal("ProvidentFund")), reader.GetDecimal(reader.GetOrdinal("ProfessionalTax")),
                                               reader.GetDecimal(reader.GetOrdinal("TaxDeducted")))
                End Using
            End Using
        End Using
    End Function

    Public Sub SavePrevEmployer(declarationId As Integer, prev As TdsPrevEmployer, userId As Integer?) Implements ITdsRepository.SavePrevEmployer
        Execute("pay.usp_TdsPrevEmployer_Save",
                Sub(cmd)
                    cmd.Parameters.Add("@DeclarationID", SqlDbType.Int).Value = declarationId
                    cmd.Parameters.Add("@EmployerName", SqlDbType.NVarChar, 150).Value = prev.EmployerName
                    cmd.Parameters.Add("@EmployerTan", SqlDbType.Char, 10).Value = If(prev.EmployerTan.Length = 0, CObj(DBNull.Value), prev.EmployerTan)
                    AddMoney(cmd, "@SalaryIncome", prev.SalaryIncome)
                    AddMoney(cmd, "@Perquisites", prev.Perquisites)
                    AddMoney(cmd, "@ProvidentFund", prev.ProvidentFund)
                    AddMoney(cmd, "@ProfessionalTax", prev.ProfessionalTax)
                    AddMoney(cmd, "@TaxDeducted", prev.TaxDeducted)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Sub ClearPrevEmployer(declarationId As Integer, userId As Integer?) Implements ITdsRepository.ClearPrevEmployer
        Execute("pay.usp_TdsPrevEmployer_Clear",
                Sub(cmd)
                    cmd.Parameters.Add("@DeclarationID", SqlDbType.Int).Value = declarationId
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Private Shared Function ReadText(reader As SqlDataReader, column As String) As String
        Dim ordinal As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(ordinal), Nothing, reader.GetString(ordinal).TrimEnd())
    End Function

    Private Shared Function ReadAmount(reader As SqlDataReader, column As String) As Decimal?
        Dim ordinal As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(ordinal), CType(Nothing, Decimal?), reader.GetDecimal(ordinal))
    End Function

    ''' <summary>A DECIMAL(14,2) parameter with its precision and scale, so SqlClient does not round the value to a whole number.</summary>
    Private Shared Sub AddMoney(cmd As SqlCommand, name As String, value As Decimal)
        Dim parameter As SqlParameter = cmd.Parameters.Add(name, SqlDbType.Decimal)
        parameter.Precision = 14
        parameter.Scale = 2
        parameter.Value = value
    End Sub

    Private Sub Execute(procedure As String, bind As Action(Of SqlCommand))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, procedure)
                    bind(cmd)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Sub
End Class
