Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>
''' ADR 0022 in a real database: who may reset whose password (sec.fn_ResettableUsers, sec.usp_User_ListResettable, sec.usp_User_ResetPassword).
''' Developer resets an Associate; an Associate resets the Owner of the open company that the Associate works on; an Owner resets an Operator of the own company.
''' Needs HRMS_TEST_CONNECTION (see Db).
''' </summary>
<Collection("User password reset")>
Public Class UserPasswordResetTests
    Implements IClassFixture(Of SeedFixture)

    Private Const NotSignedIn As Integer = 50060
    Private Const OwnPassword As Integer = 50061
    Private Const NotAllowed As Integer = 50062

    Private ReadOnly _seed As SeedFixture
    Private _developer As Integer
    Private _associateA As Integer
    Private _associateB As Integer
    Private _ownerA As Integer
    Private _ownerB As Integer
    Private _operatorA As Integer
    Private _operatorB As Integer

    Public Sub New(seed As SeedFixture)
        _seed = seed
        If Not Db.Available Then Return
        Dim tag = Guid.NewGuid().ToString("N").Substring(0, 8)
        Using admin = Db.Open()
            _developer = NewUser(admin, "pwdev_" & tag, 1, Nothing)
            _associateA = NewUser(admin, "pwassoc_a_" & tag, 3, _seed.CompanyA)
            _associateB = NewUser(admin, "pwassoc_b_" & tag, 3, _seed.CompanyB)
            Db.Execute(admin, "INSERT sec.UserCompany (UserID, CompanyID) VALUES (" & _associateA & ", " & _seed.CompanyA & "), (" & _associateB & ", " & _seed.CompanyB & ")")
            _ownerA = NewUser(admin, "pwowner_a_" & tag, 2, _seed.CompanyA)
            _ownerB = NewUser(admin, "pwowner_b_" & tag, 2, _seed.CompanyB)
            _operatorA = NewUser(admin, "pwop_a_" & tag, 4, _seed.CompanyA)
            _operatorB = NewUser(admin, "pwop_b_" & tag, 4, _seed.CompanyB)
        End Using
    End Sub

    Private Shared Function NewUser(admin As SqlConnection, userName As String, roleId As Integer, companyId As Integer?) As Integer
        Dim company = If(companyId.HasValue, companyId.Value.ToString(Globalization.CultureInfo.InvariantCulture), "NULL")
        Return CInt(Db.Scalar(admin, "INSERT sec.[User] (UserName, CompanyID, RoleID, PasswordHash, PasswordSalt, Iterations, MustChangePassword) " &
                                     "VALUES (N'" & userName & "', " & company & ", " & roleId & ", CRYPT_GEN_RANDOM(32), CRYPT_GEN_RANDOM(16), 1, 0); SELECT CAST(SCOPE_IDENTITY() AS INT);"))
    End Function

    Private Shared Function Reset(conn As SqlConnection, target As Integer) As Object
        Return Db.Proc(conn, "sec.usp_User_ResetPassword", ("@TargetUserID", target), ("@PasswordHash", New Byte(31) {}), ("@PasswordSalt", New Byte(15) {}), ("@Iterations", 7))
    End Function

    Private Shared Function Listed(conn As SqlConnection) As List(Of Integer)
        Return Db.Proc(conn, "sec.usp_User_ListResettable").Select(Function(o) CInt(o)).ToList()
    End Function

    Private Shared Sub AssertRefused(conn As SqlConnection, target As Integer, expected As Integer)
        Dim ex = Assert.Throws(Of SqlException)(Sub() Reset(conn, target))
        Assert.Equal(expected, ex.Number)
    End Sub

    <Fact>
    Public Sub Developer_ResetsAnAssociate_SetsTemporaryPassword_AndClearsTheLock()
        If Not Db.Available Then Return
        Using admin = Db.Open()
            Db.Execute(admin, "UPDATE sec.[User] SET FailedAttempts = 5, LockedUntilUtc = DATEADD(HOUR, 1, SYSUTCDATETIME()), MustChangePassword = 0 WHERE UserID = " & _associateA)
        End Using
        Using conn = Db.Open(runAs:="rv_test_app", userId:=_developer)
            Assert.Contains(_associateA, Listed(conn))
            Reset(conn, _associateA)
        End Using
        Using admin = Db.Open()
            Assert.Equal(1, CInt(Db.Scalar(admin, "SELECT CAST(MustChangePassword AS INT) FROM sec.[User] WHERE UserID = " & _associateA)))
            Assert.Equal(0, CInt(Db.Scalar(admin, "SELECT FailedAttempts FROM sec.[User] WHERE UserID = " & _associateA)))
            Assert.Equal(7, CInt(Db.Scalar(admin, "SELECT Iterations FROM sec.[User] WHERE UserID = " & _associateA)))
            Assert.True(Db.Scalar(admin, "SELECT LockedUntilUtc FROM sec.[User] WHERE UserID = " & _associateA) Is DBNull.Value)
        End Using
    End Sub

    <Fact>
    Public Sub Developer_CannotResetAnOwnerAnOperatorAnotherDeveloperOrSelf()
        If Not Db.Available Then Return
        Using conn = Db.Open(runAs:="rv_test_app", userId:=_developer)
            Dim ids = Listed(conn)
            Assert.DoesNotContain(_ownerA, ids)
            Assert.DoesNotContain(_operatorA, ids)
            AssertRefused(conn, _ownerA, NotAllowed)
            AssertRefused(conn, _operatorA, NotAllowed)
            AssertRefused(conn, _developer, OwnPassword)
        End Using
    End Sub

    <Fact>
    Public Sub Associate_ResetsTheOwnerOfTheOpenCompany_Only()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app", userId:=_associateA)
            Dim ids = Listed(conn)
            Assert.Contains(_ownerA, ids)
            Assert.DoesNotContain(_ownerB, ids)
            Assert.DoesNotContain(_operatorA, ids)
            Assert.DoesNotContain(_associateB, ids)
            Reset(conn, _ownerA)
            AssertRefused(conn, _ownerB, NotAllowed)
            AssertRefused(conn, _operatorA, NotAllowed)
            AssertRefused(conn, _associateB, NotAllowed)
            AssertRefused(conn, _associateA, OwnPassword)
        End Using
    End Sub

    <Fact>
    Public Sub Associate_CannotResetTheOwnerOfACompanyTheAssociateDoesNotWorkOn()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyB, runAs:="rv_test_app", userId:=_associateA)
            Assert.DoesNotContain(_ownerB, Listed(conn))
            AssertRefused(conn, _ownerB, NotAllowed)
        End Using
    End Sub

    <Fact>
    Public Sub Owner_ResetsAnOperatorOfTheOwnCompany_Only()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app", userId:=_ownerA)
            Dim ids = Listed(conn)
            Assert.Contains(_operatorA, ids)
            Assert.DoesNotContain(_operatorB, ids)
            Reset(conn, _operatorA)
            AssertRefused(conn, _operatorB, NotAllowed)
            AssertRefused(conn, _associateA, NotAllowed)
            AssertRefused(conn, _developer, NotAllowed)
            AssertRefused(conn, _ownerA, OwnPassword)
        End Using
    End Sub

    <Fact>
    Public Sub Owner_CannotResetAnotherOwnerEvenWithTheOtherCompanyOpen()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyB, runAs:="rv_test_app", userId:=_ownerA)
            Assert.Empty(Listed(conn))
            AssertRefused(conn, _operatorB, NotAllowed)
            AssertRefused(conn, _ownerB, NotAllowed)
        End Using
    End Sub

    <Fact>
    Public Sub Operator_ResetsNobody()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app", userId:=_operatorA)
            Assert.Empty(Listed(conn))
            AssertRefused(conn, _ownerA, NotAllowed)
        End Using
    End Sub

    <Fact>
    Public Sub WithoutASignedInUser_NothingIsListedOrReset()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Assert.Equal(NotSignedIn, Assert.Throws(Of SqlException)(Sub() Listed(conn)).Number)
            AssertRefused(conn, _ownerA, NotSignedIn)
        End Using
    End Sub

    <Fact>
    Public Sub AnInactiveUser_IsNotOnTheList()
        If Not Db.Available Then Return
        Using admin = Db.Open()
            Db.Execute(admin, "UPDATE sec.[User] SET IsActive = 0 WHERE UserID = " & _operatorB)
        End Using
        Using conn = Db.Open(_seed.CompanyB, runAs:="rv_test_app", userId:=_ownerB)
            Assert.DoesNotContain(_operatorB, Listed(conn))
            AssertRefused(conn, _operatorB, NotAllowed)
        End Using
    End Sub
End Class
