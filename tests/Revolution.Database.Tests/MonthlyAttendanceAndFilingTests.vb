Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>
''' Monthly attendance (ADR 0026, migrations 0210 to 0214) and the Monthly Pack tables (ADR 0027, migrations 0349, 0350, 0417) in a real database:
''' tenant isolation, EXECUTE-only access, the refusals, one source per month and the due date lookup. Needs HRMS_TEST_CONNECTION (see Db).
''' </summary>
Public Class MonthlyAttendanceAndFilingTests
    Implements IClassFixture(Of SeedFixture)

    Private Const ExecuteDenied As Integer = 229
    Private Const NotEmployed As Integer = 53603
    Private Const NotComplete As Integer = 53604
    Private Const OutOfRange As Integer = 53605
    Private Const OtherKindOfRows As Integer = 53602
    Private Const NeverGenerated As Integer = 53651
    Private Const FutureDate As Integer = 53652
    Private Const DueLookup As String = "DECLARE @t TABLE (DueDate DATE, HasRule BIT, IsVerified BIT, SourceNote NVARCHAR(200)); " &
                                        "INSERT @t EXEC stat.usp_DueDate_Resolve @Kind = 1, @PeriodStart = '2026-10-01'; "

    Private ReadOnly _seed As SeedFixture

    Public Sub New(seed As SeedFixture)
        _seed = seed
    End Sub

    Private Shared Function SaveArgs(employeeId As Integer, period As Date, present As Integer, hours As Decimal, complete As Boolean) As (Name As String, Value As Object)()
        Return {("@EmployeeID", CObj(employeeId)), ("@PeriodStart", CObj(period)), ("@PresentDays", CObj(present)), ("@HalfDays", CObj(0)),
                ("@WeeklyOffDays", CObj(0)), ("@HolidayDays", CObj(0)), ("@PaidLeaveDays", CObj(0)), ("@AbsentDays", CObj(0)),
                ("@OvertimeHours", CObj(hours)), ("@RequireComplete", CObj(complete))}
    End Function

    <Fact>
    Public Sub MonthlyRows_AreSeenOnlyByTheirOwnCompany_AndTheTableIsClosedToTheAppLogin()
        If Not Db.Available Then Return
        Dim period As New Date(2026, 9, 1)
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Db.Proc(conn, "att.usp_AttendanceMonthly_Save", SaveArgs(_seed.EmployeeA, period, 30, 0D, True))
            Dim ids = Db.Proc(conn, "att.usp_AttendanceMonthly_List", ("@PeriodStart", period)).Select(Function(o) CInt(o)).ToList()
            Assert.Contains(_seed.EmployeeA, ids)
            Dim denied = Assert.Throws(Of SqlException)(Sub() Db.Scalar(conn, "SELECT COUNT(*) FROM att.AttendanceMonthly"))
            Assert.Equal(ExecuteDenied, denied.Number)
        End Using
        Using conn = Db.Open(_seed.CompanyB, runAs:="rv_test_app")
            Dim ids = Db.Proc(conn, "att.usp_AttendanceMonthly_List", ("@PeriodStart", period)).Select(Function(o) CInt(o)).ToList()
            Assert.DoesNotContain(_seed.EmployeeA, ids)
        End Using
    End Sub

    <Fact>
    Public Sub Save_Refusals_HaveTheirOwnErrors()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Assert.Equal(NotEmployed, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "att.usp_AttendanceMonthly_Save", SaveArgs(_seed.EmployeeA, New Date(2019, 1, 1), 10, 0D, False))).Number)
            Assert.Equal(NotComplete, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "att.usp_AttendanceMonthly_Save", SaveArgs(_seed.EmployeeA, New Date(2026, 7, 1), 20, 0D, True))).Number)
            Assert.Equal(OutOfRange, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "att.usp_AttendanceMonthly_Save", SaveArgs(_seed.EmployeeA, New Date(2026, 7, 1), 40, 0D, False))).Number)
            Assert.Equal(OutOfRange, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "att.usp_AttendanceMonthly_Save", SaveArgs(_seed.EmployeeA, New Date(2026, 7, 1), 20, 745D, False))).Number)
            Assert.Equal(OutOfRange, Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "att.usp_AttendanceMonthly_Save", SaveArgs(_seed.EmployeeA, New Date(2026, 7, 2), 20, 0D, False))).Number)
        End Using
    End Sub

    <Fact>
    Public Sub AMonthWithMonthlyRows_RefusesDailyEntry()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Db.Proc(conn, "att.usp_AttendanceMonthly_Save", SaveArgs(_seed.EmployeeA, New Date(2026, 8, 1), 31, 0D, True))
            Dim ex = Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "att.usp_Attendance_Save", ("@EmployeeID", _seed.EmployeeA), ("@WorkDate", New Date(2026, 8, 5)),
                                                                   ("@StatusID", CByte(1)), ("@ShiftID", Nothing), ("@WorkedMinutes", CShort(480)), ("@OvertimeMinutes", CShort(0))))
            Assert.Equal(OtherKindOfRows, ex.Number)
        End Using
    End Sub

    <Fact>
    Public Sub MonthAttendanceFunction_ReadsTheMonthlyRow()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Db.Proc(conn, "att.usp_AttendanceMonthly_Save", SaveArgs(_seed.EmployeeA, New Date(2026, 6, 1), 30, 12.5D, True))
        End Using
        Using admin = Db.Open(_seed.CompanyA)
            Dim source = $"FROM att.fn_MonthAttendance({_seed.CompanyA}, '2026-06-01') WHERE EmployeeID = {_seed.EmployeeA}"
            Assert.Equal(30, CInt(Db.Scalar(admin, "SELECT PresentDays " & source)))
            Assert.Equal(750, CInt(Db.Scalar(admin, "SELECT OvertimeMinutes " & source)))
            Assert.Equal(0, CInt(Db.Scalar(admin, "SELECT UnmarkedDays " & source)))
        End Using
    End Sub

    <Fact>
    Public Sub Filing_IsPerCompany_AndNeedsGeneratingBeforeFiling()
        If Not Db.Available Then Return
        ' A new month each run, so a row left by an earlier run cannot hide the "never generated" refusal.
        Dim hash = Math.Abs(Guid.NewGuid().GetHashCode())
        Dim period As New Date(2000 + hash Mod 25, 1 + (hash \ 25) Mod 12, 1)
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Dim early = Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "pay.usp_Filing_Record", ("@PeriodStart", period), ("@Kind", CByte(1)), ("@Status", CByte(4)), ("@FiledOn", New Date(2026, 10, 10))))
            Assert.Equal(NeverGenerated, early.Number)
            Db.Proc(conn, "pay.usp_Filing_Record", ("@PeriodStart", period), ("@Kind", CByte(1)), ("@Status", CByte(3)), ("@AmountPayable", 125000.5D), ("@EmployeeCount", 40))
            Dim future = Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "pay.usp_Filing_Record", ("@PeriodStart", period), ("@Kind", CByte(1)), ("@Status", CByte(4)), ("@FiledOn", Date.Today.AddDays(3))))
            Assert.Equal(FutureDate, future.Number)
            Db.Proc(conn, "pay.usp_Filing_Record", ("@PeriodStart", period), ("@Kind", CByte(1)), ("@Status", CByte(4)), ("@FiledOn", Date.Today.AddDays(-1)), ("@ReferenceNo", "ACK123"))
            Assert.Single(Db.Proc(conn, "pay.usp_Filing_List", ("@PeriodStart", period)))
            Dim denied = Assert.Throws(Of SqlException)(Sub() Db.Scalar(conn, "SELECT COUNT(*) FROM pay.StatutoryFiling"))
            Assert.Equal(ExecuteDenied, denied.Number)
        End Using
        Using conn = Db.Open(_seed.CompanyB, runAs:="rv_test_app")
            Assert.Empty(Db.Proc(conn, "pay.usp_Filing_List", ("@PeriodStart", period)))
        End Using
    End Sub

    <Fact>
    Public Sub PackFigures_ForAMonthWithoutARun_ReturnTheCompany_AndLwfStatesRuns()
        If Not Db.Available Then Return
        Using conn = Db.Open(_seed.CompanyA, runAs:="rv_test_app")
            Dim names = Db.Proc(conn, "pay.usp_Pack_Figures", ("@PeriodStart", New Date(1999, 1, 1)))
            Assert.Single(names)
            Assert.False(String.IsNullOrWhiteSpace(CStr(names(0))))
            Db.Proc(conn, "pay.usp_Pack_LwfStates", ("@PeriodStart", New Date(2026, 10, 1)))
            Dim bad = Assert.Throws(Of SqlException)(Sub() Db.Proc(conn, "pay.usp_Pack_Figures", ("@PeriodStart", New Date(2026, 10, 5))))
            Assert.Equal(53650, bad.Number)
        End Using
    End Sub

    <Fact>
    Public Sub DueDate_UnverifiedRule_GivesNoDate_ButSaysThereIsARule()
        If Not Db.Available Then Return
        Using admin = Db.Open()
            Dim due = Db.Scalar(admin, DueLookup & "SELECT DueDate FROM @t")
            Assert.True(due Is Nothing OrElse TypeOf due Is DBNull)
            Assert.Equal(1, CInt(Db.Scalar(admin, DueLookup & "SELECT CAST(HasRule AS INT) FROM @t")))
            Assert.Equal(0, CInt(Db.Scalar(admin, DueLookup & "SELECT CAST(IsVerified AS INT) FROM @t")))
        End Using
    End Sub
End Class
