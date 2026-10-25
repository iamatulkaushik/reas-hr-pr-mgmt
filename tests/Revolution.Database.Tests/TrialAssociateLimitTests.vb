Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>
''' Migration 0027 in a real database: while an active Associate has no licence, no second Associate can be created; importing a licence
''' for the first frees the place; deactivating an Associate frees it too. Shares a collection with AssociateProvisioningTests because both
''' deactivate the test Associates before they start. Needs HRMS_TEST_CONNECTION (see Db).
''' </summary>
<Collection("Associate provisioning")>
Public Class TrialAssociateLimitTests
    Private Shared Function NewTag() As String
        Return Guid.NewGuid().ToString("N").Substring(0, 8)
    End Function

    Private Shared Sub ClearTestAssociates()
        Using admin = Db.Open()
            Db.Execute(admin, "UPDATE sec.[User] SET IsActive = 0 WHERE RoleID = 3 AND IsActive = 1 AND UserName LIKE 'assoc[_]%'")
        End Using
    End Sub

    Private Shared Function CreateUser(name As String, roleId As Byte) As Integer
        Using admin = Db.Open()
            Return CInt(Db.Proc(admin, "sec.usp_User_Create", ("@UserName", name), ("@CompanyID", CType(Nothing, Object)), ("@RoleID", roleId),
                                ("@PasswordHash", New Byte(31) {}), ("@PasswordSalt", New Byte(15) {}), ("@Iterations", 1), ("@MustChangePassword", False))(0))
        End Using
    End Function

    Private Shared Sub ImportLicence(developerId As Integer, associateName As String)
        Using conn = Db.Open(userId:=developerId)
            Db.Proc(conn, "cfg.usp_Licence_Import", ("@LicenceGuid", Guid.NewGuid()), ("@AssociateUserName", associateName), ("@LicenceText", "test"),
                    ("@ValidFrom", New Date(2026, 1, 1)), ("@ValidTo", New Date(2036, 1, 1)), ("@PlanCapTierID", CByte(1)),
                    ("@MaxCompanies", CShort(5)), ("@MaxActiveUsers", 10))
        End Using
    End Sub

    <Fact>
    Public Sub SecondAssociate_IsRefused_WhileTheFirstHasNoLicence()
        If Not Db.Available Then Return
        ClearTestAssociates()
        Dim tag = NewTag()
        CreateUser("assoc_" & tag & "_1", 3)
        Dim refused = Assert.Throws(Of SqlException)(Sub() CreateUser("assoc_" & tag & "_2", 3))
        Assert.Equal(50055, refused.Number)
        Using admin = Db.Open()
            Assert.Equal(0, CInt(Db.Scalar(admin, "SELECT COUNT(*) FROM sec.[User] WHERE UserName = 'assoc_" & tag & "_2'")))
        End Using
    End Sub

    <Fact>
    Public Sub SecondAssociate_IsAllowed_OnceTheFirstHasALicence()
        If Not Db.Available Then Return
        ClearTestAssociates()
        Dim tag = NewTag()
        Dim developerId = CreateUser("dev_" & tag, 1)
        CreateUser("assoc_" & tag & "_1", 3)
        ImportLicence(developerId, "assoc_" & tag & "_1")
        Assert.True(CreateUser("assoc_" & tag & "_2", 3) > 0)
    End Sub

    <Fact>
    Public Sub DeactivatingTheAssociate_FreesThePlace()
        If Not Db.Available Then Return
        ClearTestAssociates()
        Dim tag = NewTag()
        Dim first = CreateUser("assoc_" & tag & "_1", 3)
        Using admin = Db.Open()
            Db.Execute(admin, "UPDATE sec.[User] SET IsActive = 0 WHERE UserID = " & first.ToString())
        End Using
        Assert.True(CreateUser("assoc_" & tag & "_2", 3) > 0)
    End Sub

    <Fact>
    Public Sub OtherRoles_AreNotLimitedByTheTrial()
        If Not Db.Available Then Return
        ClearTestAssociates()
        Dim tag = NewTag()
        CreateUser("assoc_" & tag, 3)
        Assert.True(CreateUser("dev_" & tag, 1) > 0)
    End Sub
End Class
