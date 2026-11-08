Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>
''' Change requests 0023 and 0025 in a real database: the punch agent reaches the server only through the agt procedures, with a key bound to one company, and the
''' ingest procedure is closed to the app login (migration 0030, and database/special/biometric 0900 and 0901). Needs HRMS_TEST_CONNECTION (see Db).
''' The tests that need the special build return at once when it is not installed (ADR 0021); the app-login denial is checked either way.
''' </summary>
<Collection("Agent wrapper")>
Public Class AgentWrapperTests
    Implements IClassFixture(Of SeedFixture)

    Private Const Refused As Integer = 53400
    Private Const NotAllowed As Integer = 53410
    Private Const ExecuteDenied As Integer = 229

    Private ReadOnly _seed As SeedFixture
    Private _ownerA As Integer
    Private _ownerB As Integer
    Private _operatorA As Integer
    Private _keyA As String
    Private _keyAId As Integer
    Private _deviceA As Integer
    Private _deviceB As Integer

    Public Sub New(seed As SeedFixture)
        _seed = seed
        If Not Installed Then Return
        Dim tag = Guid.NewGuid().ToString("N").Substring(0, 8)
        Using admin = Db.Open()
            Db.Execute(admin, "IF DATABASE_PRINCIPAL_ID('rv_test_agent') IS NULL CREATE USER rv_test_agent WITHOUT LOGIN; ALTER ROLE hrms_agent ADD MEMBER rv_test_agent;")
            _ownerA = NewUser(admin, "agentowner_a_" & tag, 2, _seed.CompanyA)
            _ownerB = NewUser(admin, "agentowner_b_" & tag, 2, _seed.CompanyB)
            _operatorA = NewUser(admin, "agentop_a_" & tag, 4, _seed.CompanyA)
        End Using
        Dim issuedA = Issue(_seed.CompanyA, _ownerA, "Works PC A")
        _keyAId = issuedA.Id
        _keyA = issuedA.Key
        _deviceA = MakeDevice(_seed.CompanyA, _seed.FactoryA, "ADEV")
        _deviceB = MakeDevice(_seed.CompanyB, _seed.FactoryB, "BDEV")
    End Sub

    ''' <summary>True when a database is configured and the biometric special build (schema agt, table sec.AgentRegistration) is applied to it.</summary>
    Private Shared ReadOnly Property Installed As Boolean
        Get
            If Not Db.Available Then Return False
            Using admin = Db.Open()
                Return CInt(Db.Scalar(admin, "SELECT CASE WHEN SCHEMA_ID(N'agt') IS NOT NULL AND OBJECT_ID(N'sec.AgentRegistration', N'U') IS NOT NULL THEN 1 ELSE 0 END")) = 1
            End Using
        End Get
    End Property

    Private Shared Function NewUser(admin As SqlConnection, userName As String, roleId As Integer, companyId As Integer) As Integer
        Return CInt(Db.Scalar(admin, "INSERT sec.[User] (UserName, CompanyID, RoleID, PasswordHash, PasswordSalt, Iterations, MustChangePassword) " &
                                     "VALUES (N'" & userName & "', " & companyId & ", " & roleId & ", CRYPT_GEN_RANDOM(32), CRYPT_GEN_RANDOM(16), 1, 0); SELECT CAST(SCOPE_IDENTITY() AS INT);"))
    End Function

    Private Shared Function Issue(companyId As Integer, userId As Integer, label As String) As (Id As Integer, Key As String)
        Using conn = Db.Open(companyId, runAs:="rv_test_app", userId:=userId)
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "sec.usp_Agent_Issue"
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.AddWithValue("@Label", label)
                Using r = cmd.ExecuteReader()
                    r.Read()
                    Return (r.GetInt32(0), r.GetString(1))
                End Using
            End Using
        End Using
    End Function

    Private Shared Function MakeDevice(companyId As Integer, factoryId As Integer, code As String) As Integer
        Using conn = Db.Open(companyId, runAs:="rv_test_app")
            Return CInt(Db.Proc(conn, "att.usp_Device_Save", ("@FactoryID", factoryId), ("@DeviceCode", code), ("@DeviceName", "Gate " & code),
                                ("@Brand", "ESSL"), ("@Host", "192.168.1.50"), ("@Port", 4370))(0))
        End Using
    End Function

    Private Shared Function OnePunch() As String
        Return "[{""deviceUserId"":7,""punchTime"":""" & DateTime.Now.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss") & """}]"
    End Function

    Private Shared Function AsAgent() As SqlConnection
        Return Db.Open(runAs:="rv_test_agent")
    End Function

    <Fact>
    Public Sub IssuedKey_IsBase64Of32Bytes_AndOnlyItsHashIsStored()
        If Not Installed Then Return
        Assert.Equal(44, _keyA.Length)
        Assert.Equal(32, Convert.FromBase64String(_keyA).Length)
        Using admin = Db.Open()
            Dim hash = CType(Db.Scalar(admin, "SELECT KeyHash FROM sec.AgentRegistration WHERE AgentRegistrationID = " & _keyAId), Byte())
            Assert.Equal(System.Security.Cryptography.SHA256.HashData(Convert.FromBase64String(_keyA)), hash)
        End Using
    End Sub

    <Fact>
    Public Sub AnOperator_CannotIssueListOrRetireKeys()
        If Not Installed Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app", userId:=_operatorA)
            Assert.Equal(NotAllowed, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "sec.usp_Agent_Issue", ("@Label", "x"))).Number)
            Assert.Equal(NotAllowed, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "sec.usp_Agent_List")).Number)
            Assert.Equal(NotAllowed, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "sec.usp_Agent_Retire", ("@AgentRegistrationID", _keyAId))).Number)
        End Using
    End Sub

    <Fact>
    Public Sub TheOwnerOfAnotherCompany_CannotRetireOrListThisCompanysKeys()
        If Not Installed Then Return
        Using conn = Db.Open(_seed.CompanyB, runAs:="rv_test_app", userId:=_ownerB)
            Assert.Equal(53412, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "sec.usp_Agent_Retire", ("@AgentRegistrationID", _keyAId))).Number)
            Assert.DoesNotContain(_keyAId, Db.Proc(conn, "sec.usp_Agent_List").Select(Function(o) CInt(o)).ToList())
        End Using
    End Sub

    <Fact>
    Public Sub Devices_AreOnlyThoseOfTheKeysCompany()
        If Not Installed Then Return
        Using conn = AsAgent()
            Dim codes = Db.Proc(conn, "agt.usp_Agent_Devices", ("@AgentKey", _keyA)).Select(Function(o) CStr(o)).ToList()
            Assert.Contains("ADEV", codes)
            Assert.DoesNotContain("BDEV", codes)
        End Using
    End Sub

    <Fact>
    Public Sub Ingest_StoresThePunchUnderTheKeysCompany_Only()
        If Not Installed Then Return
        Using conn = AsAgent()
            Dim received = CInt(Db.Proc(conn, "agt.usp_Agent_Ingest", ("@AgentKey", _keyA), ("@DeviceCode", "ADEV"), ("@PunchesJson", OnePunch()))(0))
            Assert.Equal(1, received)
        End Using
        Using admin = Db.Open()
            Assert.True(CInt(Db.Scalar(admin, "SELECT COUNT(*) FROM att.Punch WHERE CompanyID = " & _seed.CompanyA & " AND DeviceID = " & _deviceA)) >= 1)
            Assert.Equal(0, CInt(Db.Scalar(admin, "SELECT COUNT(*) FROM att.Punch WHERE CompanyID = " & _seed.CompanyB & " AND DeviceID = " & _deviceA)))
        End Using
    End Sub

    <Fact>
    Public Sub TheKeyOfCompanyA_CannotUseTheDeviceCodeOfCompanyB()
        If Not Installed Then Return
        Using conn = AsAgent()
            Assert.Equal(Refused, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "agt.usp_Agent_Ingest", ("@AgentKey", _keyA), ("@DeviceCode", "BDEV"), ("@PunchesJson", OnePunch()))).Number)
        End Using
        Using admin = Db.Open()
            Assert.Equal(0, CInt(Db.Scalar(admin, "SELECT COUNT(*) FROM att.Punch WHERE DeviceID = " & _deviceB)))
        End Using
    End Sub

    <Fact>
    Public Sub WrongMalformedAndEmptyKeys_AreAllRefusedTheSameWay()
        If Not Installed Then Return
        Dim wrong = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
        Dim shortKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16))
        Using conn = AsAgent()
            For Each candidate As String In New String() {wrong, shortKey, "not base64 at all, not base64 at all!!!!!!!", "", _keyA.Substring(1)}
                Dim key = candidate
                Dim ex = Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "agt.usp_Agent_Devices", ("@AgentKey", key)))
                Assert.Equal(Refused, ex.Number)
            Next
            Assert.Equal(Refused, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "agt.usp_Agent_Devices", ("@AgentKey", Nothing))).Number)
        End Using
    End Sub

    <Fact>
    Public Sub ARetiredKey_IsRefused()
        If Not Installed Then Return
        Dim issued = Issue(_seed.CompanyA, _ownerA, "To retire")
        Using conn = AsAgent()
            Assert.NotEmpty(Db.Proc(conn, "agt.usp_Agent_Devices", ("@AgentKey", issued.Key)))
        End Using
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app", userId:=_ownerA)
            Db.Proc(conn, "sec.usp_Agent_Retire", ("@AgentRegistrationID", issued.Id))
        End Using
        Using conn = AsAgent()
            Assert.Equal(Refused, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "agt.usp_Agent_Devices", ("@AgentKey", issued.Key))).Number)
        End Using
    End Sub

    <Fact>
    Public Sub TheAppLogin_CannotCallThePunchIngestProcedure_ForAnyCompany()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Dim ex = Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "att.usp_Punch_Ingest", ("@CompanyID", _seed.CompanyB), ("@DeviceID", _deviceB), ("@PunchesJson", OnePunch())))
            Assert.Equal(ExecuteDenied, ex.Number)
        End Using
    End Sub

    <Fact>
    Public Sub TheAppLogin_CannotCallTheAgentProcedures()
        If Not Installed Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Assert.Equal(ExecuteDenied, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "agt.usp_Agent_Devices", ("@AgentKey", _keyA))).Number)
        End Using
    End Sub

    <Fact>
    Public Sub TheAgentLogin_HasExecuteOnAgtOnly()
        If Not Installed Then Return
        Using conn = AsAgent()
            Assert.Equal(ExecuteDenied, Assert.Throws(Of SqlException)(Sub() Db.Scalar(conn, "SELECT COUNT(*) FROM att.Punch")).Number)
            Assert.Equal(ExecuteDenied, Assert.Throws(Of SqlException)(Sub() Db.Scalar(conn, "SELECT COUNT(*) FROM sec.AgentRegistration")).Number)
            Assert.Equal(ExecuteDenied, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "att.usp_Punch_Ingest", ("@CompanyID", _seed.CompanyA), ("@DeviceID", _deviceA), ("@PunchesJson", OnePunch()))).Number)
            Assert.Equal(ExecuteDenied, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "agt.usp_Agent_Authenticate", ("@AgentKey", _keyA), ("@CompanyID", 0))).Number)
            Assert.Equal(ExecuteDenied, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "mst.usp_Employee_List", ("@ActiveOnly", False))).Number)
        End Using
    End Sub

    <Fact>
    Public Sub FactoryList_ForTheLocationPicker_ShowsOnlyTheOwnCompanysActiveLocations()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Dim ids = Db.Proc(conn, "mst.usp_Factory_List", ("@ActiveOnly", True)).Select(Function(o) CInt(o)).ToList()
            Assert.Contains(_seed.FactoryA, ids)
            Assert.DoesNotContain(_seed.FactoryB, ids)
        End Using
    End Sub
End Class
