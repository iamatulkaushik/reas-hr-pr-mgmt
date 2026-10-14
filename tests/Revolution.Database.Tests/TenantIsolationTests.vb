Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>
''' Proves in a real database what the unit tests cannot: tenant isolation, EXECUTE-only access and transaction safety.
''' Needs HRMS_TEST_CONNECTION (see Db). Run RevolutionSetup migrate on the test database first.
''' </summary>
Public Class TenantIsolationTests
    Implements IClassFixture(Of SeedFixture)

    Private ReadOnly _seed As SeedFixture

    Public Sub New(seed As SeedFixture)
        _seed = seed
    End Sub

    <Fact>
    Public Sub DatabaseTests_AreConfigured_WhenRequired()
        If Db.Required Then
            Assert.True(Db.Available, "HRMS_REQUIRE_DB_TESTS is 1 but HRMS_TEST_CONNECTION is not set.")
        End If
    End Sub

    <Fact>
    Public Sub EveryTableWithCompanyId_HasAnEnabledFilterPolicy()
        If Not Db.Available Then Return
        Using conn = Db.Open()
            Dim missing As New List(Of String)()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "SELECT s.name + '.' + t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id " &
                                  "JOIN sys.columns c ON c.object_id = t.object_id AND c.name = 'CompanyID' " &
                                  "WHERE s.name + '.' + t.name NOT IN ('mst.Company', 'sec.User', 'sec.UserCompany') AND NOT EXISTS " &
                                  "(SELECT 1 FROM sys.security_predicates p JOIN sys.security_policies sp ON sp.object_id = p.object_id " &
                                  " WHERE p.target_object_id = t.object_id AND p.predicate_type = 0 AND sp.is_enabled = 1)"
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        missing.Add(r.GetString(0))
                    End While
                End Using
            End Using
            Assert.Empty(missing)
        End Using
    End Sub

    <Fact>
    Public Sub EmployeeList_ShowsOnlyTheOwnCompany()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Dim ids = Db.Proc(conn, "mst.usp_Employee_List", ("@ActiveOnly", False)).Select(Function(o) CInt(o)).ToList()
            Assert.Contains(_seed.EmployeeA, ids)
            Assert.DoesNotContain(_seed.EmployeeB, ids)
        End Using
    End Sub

    <Fact>
    Public Sub EmployeeGetById_OfAnotherCompany_ReturnsNothing()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyB, runAs:="rv_test_app")
            Assert.Empty(Db.Proc(conn, "mst.usp_Employee_GetById", ("@EmployeeID", _seed.EmployeeA)))
        End Using
    End Sub

    <Fact>
    Public Sub EmployeeUpdate_OfAnotherCompany_Fails_NotFound()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyB, runAs:="rv_test_app")
            Dim ex = Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "mst.usp_Employee_Update", ("@EmployeeID", _seed.EmployeeA),
                ("@FullName", "Taken over"), ("@Gender", "M"), ("@DateOfBirth", New Date(1990, 1, 1)), ("@JoinDate", New Date(2020, 4, 1)),
                ("@EmploymentTypeID", CByte(1)), ("@PayTypeID", CByte(2)), ("@FactoryID", _seed.FactoryB)))
            Assert.Equal(51002, ex.Number)
        End Using
    End Sub

    <Fact>
    Public Sub NoCompanyInSession_Fails()
        If Not Db.Available Then Return
        Using conn = Db.Open(runAs:="rv_test_app")
            Dim ex = Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "mst.usp_Employee_List"))
            Assert.Equal(50002, ex.Number)
        End Using
    End Sub

    <Fact>
    Public Sub AppRole_CannotReadTablesDirectly()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Dim ex = Assert.Throws(Of SqlException)(Sub() Db.Scalar(conn, "SELECT COUNT(*) FROM mst.Employee"))
            Assert.Equal(229, ex.Number)
        End Using
    End Sub

    <Fact>
    Public Sub RowFilter_HidesOtherCompanyRows_ForADirectReader()
        If Not Db.Available Then Return
        Dim sql = "SELECT COUNT(*) FROM mst.Employee WHERE EmployeeID = " & _seed.EmployeeA
        Using asA = Db.Open(_seed.CompanyA, runAs:="rv_test_reader")
            Assert.Equal(1, CInt(Db.Scalar(asA, sql)))
        End Using
        Using asB = Db.Open(_seed.CompanyB, runAs:="rv_test_reader")
            Assert.Equal(0, CInt(Db.Scalar(asB, sql)))
        End Using
        Using noCompany = Db.Open(runAs:="rv_test_reader")
            Assert.Equal(0, CInt(Db.Scalar(noCompany, sql)))
        End Using
    End Sub

    <Fact>
    Public Sub DeveloperSession_SeesAllCompanies()
        If Not Db.Available Then Return
        Using conn = Db.Open(developer:=True, runAs:="rv_test_reader")
            Dim sql = "SELECT COUNT(*) FROM mst.Employee WHERE EmployeeID IN (" & _seed.EmployeeA & ", " & _seed.EmployeeB & ")"
            Assert.Equal(2, CInt(Db.Scalar(conn, sql)))
        End Using
    End Sub

    <Fact>
    Public Sub CompositeKey_RefusesAnEmployeeWithAnotherCompanysFactory()
        If Not Db.Available Then Return
        Using admin = Db.Open()
            Dim ex = Assert.Throws(Of SqlException)(Sub() Db.Execute(admin,
                "INSERT mst.Employee (CompanyID, EmpCode, FullName, Gender, DateOfBirth, JoinDate, EmploymentTypeID, PayTypeID, FactoryID) " &
                "VALUES (" & _seed.CompanyA & ", 'CROSS1', 'Cross', 'M', '1990-01-01', '2020-04-01', 1, 2, " & _seed.FactoryB & ")"))
            Assert.Equal(547, ex.Number)
        End Using
    End Sub

    <Fact>
    Public Sub EmployeeCode_IsUniquePerCompany_NotAcrossCompanies()
        If Not Db.Available Then Return
        Dim code = "DUP" & Guid.NewGuid().ToString("N").Substring(0, 6)
        _seed.MakeEmployee(_seed.CompanyA, _seed.FactoryA, "Same code A", code)
        _seed.MakeEmployee(_seed.CompanyB, _seed.FactoryB, "Same code B", code)
        Dim ex = Assert.Throws(Of SqlException)(Sub() _seed.MakeEmployee(_seed.CompanyA, _seed.FactoryA, "Same code again", code))
        Assert.True(ex.Number = 2627 OrElse ex.Number = 2601)
    End Sub

    <Fact>
    Public Sub SamePanHash_InOneCompany_IsRefused()
        If Not Db.Available Then Return
        Dim first = _seed.MakeEmployee(_seed.CompanyA, _seed.FactoryA, "Pan one", Nothing)
        Dim second = _seed.MakeEmployee(_seed.CompanyA, _seed.FactoryA, "Pan two", Nothing)
        Dim hash = New Byte(31) {}
        Guid.NewGuid().ToByteArray().CopyTo(hash, 0)
        Dim cipher = New Byte() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42}
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Db.Proc(conn, "mst.usp_EmployeeStatutory_SetIdentifiers", ("@EmployeeID", first), ("@IdKeyVersion", CByte(1)), ("@SetPan", True),
                    ("@PanEnc", cipher), ("@PanHash", hash), ("@PanLast4", "234F"))
            Dim ex = Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "mst.usp_EmployeeStatutory_SetIdentifiers", ("@EmployeeID", second),
                ("@IdKeyVersion", CByte(1)), ("@SetPan", True), ("@PanEnc", cipher), ("@PanHash", hash), ("@PanLast4", "234F")))
            Assert.True(ex.Number = 2601 OrElse ex.Number = 2627)
        End Using
    End Sub

    <Fact>
    Public Sub MonthClose_Twice_FailsAndLeavesNoOpenTransaction()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Db.Proc(conn, "att.usp_Month_Close", ("@MonthKey", 202601))
            Dim ex = Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "att.usp_Month_Close", ("@MonthKey", 202601)))
            Assert.Equal(50003, ex.Number)
            Assert.Equal(0, CInt(Db.Scalar(conn, "SELECT @@TRANCOUNT")))
        End Using
    End Sub
End Class
