Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>
''' Migration 0028 in a real database: the absence deduction divisor defaults to 30, the optional save parameter keeps the stored value, and a divisor of
''' zero is refused. Needs HRMS_TEST_CONNECTION (see Db). Run RevolutionSetup migrate on the test database first.
''' </summary>
Public Class PolicyAbsenceDivisorTests
    Implements IClassFixture(Of SeedFixture)

    Private Const SaveWithoutDivisor As String =
        "EXEC pay.usp_Policy_Save @PayCycleStartDay = 1, @OtMultiplier = 2, @OtInEpfWage = 0, @OtInEsiWage = 0, @DailyWageDivisor = 26, @EmiCapPct = 25"

    Private ReadOnly _seed As SeedFixture

    Public Sub New(seed As SeedFixture)
        _seed = seed
    End Sub

    Private Function StoredDivisor(conn As SqlConnection) As Decimal
        Return Convert.ToDecimal(Db.Scalar(conn, $"SELECT AbsenceDeductionDivisor FROM pay.CompanyPolicy WHERE CompanyID = {_seed.CompanyA}"))
    End Function

    <Fact>
    Public Sub NewPolicy_GetsAnAbsenceDivisorOfThirty()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA)
            Db.Execute(conn, $"DELETE FROM pay.CompanyPolicy WHERE CompanyID = {_seed.CompanyA}")
            Db.Execute(conn, "EXEC pay.usp_Policy_Get")
            Assert.Equal(30D, StoredDivisor(conn))
        End Using
    End Sub

    <Fact>
    Public Sub Save_WithTheDivisor_StoresIt_AndWithoutItKeepsIt()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA)
            Db.Execute(conn, SaveWithoutDivisor & ", @AbsenceDeductionDivisor = 28")
            Assert.Equal(28D, StoredDivisor(conn))
            Db.Execute(conn, SaveWithoutDivisor)
            Assert.Equal(28D, StoredDivisor(conn))
        End Using
    End Sub

    <Fact>
    Public Sub Save_WithADivisorOfZero_IsRefused()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA)
            Assert.Throws(Of SqlException)(Sub() Db.Execute(conn, SaveWithoutDivisor & ", @AbsenceDeductionDivisor = 0"))
        End Using
    End Sub
End Class
