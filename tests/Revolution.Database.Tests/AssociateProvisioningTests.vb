Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>
''' ADR 0013 in a real database: an Associate links to companies, the app can create a company and an Owner login only through the
''' two Associate procedures, and those procedures check the caller in the database. Needs HRMS_TEST_CONNECTION (see Db).
''' </summary>
<Collection("Associate provisioning")>
Public Class AssociateProvisioningTests
    Implements IClassFixture(Of SeedFixture)

    Private ReadOnly _seed As SeedFixture

    Public Sub New(seed As SeedFixture)
        _seed = seed
    End Sub

    Private Shared Function NewTag() As String
        Return Guid.NewGuid().ToString("N").Substring(0, 8)
    End Function

    ''' <summary>Migration 0027: the trial allows one unlicensed Associate, so the Associates of earlier tests are deactivated first.</summary>
    Private Shared Function CreateAssociate(tag As String) As Integer
        Using admin = Db.Open()
            Db.Execute(admin, "UPDATE sec.[User] SET IsActive = 0 WHERE RoleID = 3 AND IsActive = 1 AND UserName LIKE 'assoc[_]%'")
            Return CInt(Db.Proc(admin, "sec.usp_User_Create", ("@UserName", "assoc_" & tag), ("@CompanyID", CType(Nothing, Object)),
                                ("@RoleID", CByte(3)), ("@PasswordHash", New Byte(31) {}), ("@PasswordSalt", New Byte(15) {}),
                                ("@Iterations", 1), ("@MustChangePassword", False))(0))
        End Using
    End Function

    <Fact>
    Public Sub Associate_CreatesACompany_AndIsLinkedToIt()
        If Not Db.Available Then Return
        Dim userId = CreateAssociate(NewTag())
        Dim companyId As Integer
        Using conn = Db.Open(userId:=userId, runAs:="rv_test_app")
            companyId = CInt(Db.Proc(conn, "sec.usp_Associate_CreateCompany", ("@CompanyName", "  Assoc made " & NewTag() & "  "))(0))
        End Using
        Using conn = Db.Open(userId:=userId, runAs:="rv_test_app")
            Dim linked = Db.Proc(conn, "sec.usp_UserCompany_List", ("@UserID", userId)).Select(Function(o) CInt(o)).ToList()
            Assert.Contains(companyId, linked)
        End Using
        Using admin = Db.Open()
            Assert.Equal(1, CInt(Db.Scalar(admin, "SELECT PlanTierID FROM mst.Company WHERE CompanyID = " & companyId.ToString())))
        End Using
    End Sub

    <Fact>
    Public Sub CreateCompany_ByAnyoneButAnAssociate_IsRefused()
        If Not Db.Available Then Return
        Dim ex = Assert.Throws(Of SqlException)(Sub()
                                                    Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
                                                        Db.Proc(conn, "sec.usp_Associate_CreateCompany", ("@CompanyName", "Nope"))
                                                    End Using
                                                End Sub)
        Assert.Equal(50030, ex.Number)
    End Sub

    <Fact>
    Public Sub CreateOwner_OnlyForACompanyTheAssociateWorksOn()
        If Not Db.Available Then Return
        Dim tag = NewTag()
        Dim userId = CreateAssociate(tag)
        Dim own As Integer
        Using conn = Db.Open(userId:=userId, runAs:="rv_test_app")
            own = CInt(Db.Proc(conn, "sec.usp_Associate_CreateCompany", ("@CompanyName", "Own " & tag))(0))
            Dim ownerId = CInt(Db.Proc(conn, "sec.usp_Associate_CreateOwner", ("@CompanyID", own), ("@UserName", "owner_" & tag),
                                       ("@PasswordHash", New Byte(31) {}), ("@PasswordSalt", New Byte(15) {}), ("@Iterations", 1))(0))
            Assert.True(ownerId > 0)
        End Using
        Dim refused = Assert.Throws(Of SqlException)(Sub()
                                                         Using conn = Db.Open(userId:=userId, runAs:="rv_test_app")
                                                             Db.Proc(conn, "sec.usp_Associate_CreateOwner", ("@CompanyID", _seed.CompanyA), ("@UserName", "owner2_" & tag),
                                                                     ("@PasswordHash", New Byte(31) {}), ("@PasswordSalt", New Byte(15) {}), ("@Iterations", 1))
                                                         End Using
                                                     End Sub)
        Assert.Equal(50032, refused.Number)
        Using admin = Db.Open()
            Assert.Equal(2, CInt(Db.Scalar(admin, "SELECT RoleID FROM sec.[User] WHERE UserName = 'owner_" & tag & "'")))
            Assert.Equal(True, CBool(Db.Scalar(admin, "SELECT MustChangePassword FROM sec.[User] WHERE UserName = 'owner_" & tag & "'")))
        End Using
    End Sub

    <Fact>
    Public Sub AppRole_CannotUseTheSetupProcedures()
        If Not Db.Available Then Return
        Assert.Throws(Of SqlException)(Sub()
                                           Using conn = Db.Open(runAs:="rv_test_app")
                                               Db.Proc(conn, "mst.usp_Company_Create", ("@CompanyName", "Nope"), ("@PlanTierID", CByte(1)))
                                           End Using
                                       End Sub)
    End Sub

    <Fact>
    Public Sub CreateOwner_SecondOwnerForTheSameCompany_IsRefused()
        If Not Db.Available Then Return
        Dim tag = NewTag()
        Dim userId = CreateAssociate(tag)
        Dim company As Integer
        Using conn = Db.Open(userId:=userId, runAs:="rv_test_app")
            company = CInt(Db.Proc(conn, "sec.usp_Associate_CreateCompany", ("@CompanyName", "One owner " & tag))(0))
            Db.Proc(conn, "sec.usp_Associate_CreateOwner", ("@CompanyID", company), ("@UserName", "first_" & tag),
                    ("@PasswordHash", New Byte(31) {}), ("@PasswordSalt", New Byte(15) {}), ("@Iterations", 1))
        End Using
        Dim refused = Assert.Throws(Of SqlException)(Sub()
                                                         Using conn = Db.Open(userId:=userId, runAs:="rv_test_app")
                                                             Db.Proc(conn, "sec.usp_Associate_CreateOwner", ("@CompanyID", company), ("@UserName", "second_" & tag),
                                                                     ("@PasswordHash", New Byte(31) {}), ("@PasswordSalt", New Byte(15) {}), ("@Iterations", 1))
                                                         End Using
                                                     End Sub)
        Assert.Equal(50033, refused.Number)
    End Sub

    <Fact>
    Public Sub CreateCompany_StopsAtThePlanQuota()
        If Not Db.Available Then Return
        Dim tag = NewTag()
        Dim userId = CreateAssociate(tag)
        Using conn = Db.Open(userId:=userId, runAs:="rv_test_app")
            For i = 1 To 5
                Db.Proc(conn, "sec.usp_Associate_CreateCompany", ("@CompanyName", "Quota " & tag & " " & i.ToString()))
            Next
        End Using
        Dim refused = Assert.Throws(Of SqlException)(Sub()
                                                         Using conn = Db.Open(userId:=userId, runAs:="rv_test_app")
                                                             Db.Proc(conn, "sec.usp_Associate_CreateCompany", ("@CompanyName", "Quota " & tag & " 6"))
                                                         End Using
                                                     End Sub)
        Assert.Equal(50034, refused.Number)
        Using admin = Db.Open()
            Db.Proc(admin, "sec.usp_Associate_SetPlan", ("@UserID", userId), ("@PlanTierID", CByte(2)))
        End Using
        Using conn = Db.Open(userId:=userId, runAs:="rv_test_app")
            Assert.True(CInt(Db.Proc(conn, "sec.usp_Associate_CreateCompany", ("@CompanyName", "Quota " & tag & " 6"))(0)) > 0)
        End Using
    End Sub

    <Fact>
    Public Sub PlanQuota_HasFourFiniteLimits()
        If Not Db.Available Then Return
        Using admin = Db.Open()
            Assert.Equal(4, CInt(Db.Scalar(admin, "SELECT COUNT(*) FROM cfg.PlanQuota WHERE MaxCompanies BETWEEN 1 AND 500")))
            Assert.Equal(90, CInt(Db.Scalar(admin, "SELECT MaxCompanies FROM cfg.PlanQuota WHERE PlanTierID = 4")))
        End Using
    End Sub

    <Fact>
    Public Sub AppRole_CannotSetAnAssociatePlan()
        If Not Db.Available Then Return
        Assert.Throws(Of SqlException)(Sub()
                                           Using conn = Db.Open(runAs:="rv_test_app")
                                               Db.Proc(conn, "sec.usp_Associate_SetPlan", ("@UserID", 1), ("@PlanTierID", CByte(4)))
                                           End Using
                                       End Sub)
    End Sub
End Class
