Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Bank storage through mst.usp_EmployeeBank_* (ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlEmployeeBankRepository
    Implements IEmployeeBankRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function GetBank(employeeId As Integer) As BankView Implements IEmployeeBankRepository.GetBank
        Dim found As BankView = Nothing
        EmployeeSql.Run(Sub()
                            Using conn = _factory.Open()
                                Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_EmployeeBank_Get")
                                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                    Using reader = cmd.ExecuteReader()
                                        If reader.Read() Then
                                            found = New BankView With {
                                                .PaymentMode = CType(CInt(reader.GetByte(reader.GetOrdinal("PaymentModeID"))), PaymentMode),
                                                .BankId = StoredProcedure.ReadNullableInt(reader, "BankID"),
                                                .Ifsc = EmployeeSql.OptText(reader, "Ifsc"),
                                                .AccountLast4 = EmployeeSql.OptText(reader, "AccountLast4")}
                                        End If
                                    End Using
                                End Using
                            End Using
                        End Sub)
        Return found
    End Function

    Public Function GetEncryptedAccount(employeeId As Integer) As EncryptedBankAccount Implements IEmployeeBankRepository.GetEncryptedAccount
        Dim found As EncryptedBankAccount = Nothing
        EmployeeSql.Run(Sub()
                            Using conn = _factory.Open()
                                Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_EmployeeBank_GetEncrypted")
                                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                    Using reader = cmd.ExecuteReader()
                                        If reader.Read() Then
                                            found = New EncryptedBankAccount(reader.GetByte(reader.GetOrdinal("IdKeyVersion")),
                                                                             EmployeeSql.OptBytes(reader, "AccountEnc"))
                                        End If
                                    End Using
                                End Using
                            End Using
                        End Sub)
        Return found
    End Function

    Public Sub SaveBank(employeeId As Integer, write As BankWrite, userId As Integer?) Implements IEmployeeBankRepository.SaveBank
        If write Is Nothing Then Throw New ArgumentNullException(NameOf(write))
        Dim account As ProtectedId = write.Account.Value
        EmployeeSql.Execute(_factory, "mst.usp_EmployeeBank_Save",
                            Sub(cmd)
                                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                cmd.Parameters.Add("@PaymentModeID", SqlDbType.TinyInt).Value = CByte(CInt(write.PaymentMode))
                                cmd.Parameters.Add("@BankID", SqlDbType.Int).Value = StoredProcedure.DbValue(write.BankId)
                                cmd.Parameters.Add("@Ifsc", SqlDbType.Char, 11).Value = If(write.Ifsc.Length = 0, CObj(DBNull.Value), write.Ifsc)
                                cmd.Parameters.Add("@IdKeyVersion", SqlDbType.TinyInt).Value = write.KeyVersion
                                cmd.Parameters.Add("@SetAccount", SqlDbType.Bit).Value = write.Account.Kind <> IdChangeKind.Unchanged
                                cmd.Parameters.Add("@AccountEnc", SqlDbType.VarBinary, 256).Value = If(account Is Nothing, CObj(DBNull.Value), CObj(account.Cipher))
                                cmd.Parameters.Add("@AccountLast4", SqlDbType.Char, 4).Value = If(account Is Nothing, CObj(DBNull.Value), account.Last4)
                                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                            End Sub)
    End Sub

    Public Sub LogReveal(userId As Integer, employeeId As Integer, fields As String) Implements IEmployeeBankRepository.LogReveal
        EmployeeSql.Execute(_factory, "aud.usp_Reveal_Insert",
                            Sub(cmd)
                                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId
                                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                cmd.Parameters.Add("@Fields", SqlDbType.NVarChar, 100).Value = fields
                                cmd.Parameters.Add("@RowsRevealed", SqlDbType.Int).Value = 1
                            End Sub)
    End Sub
End Class
