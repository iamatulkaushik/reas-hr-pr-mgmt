Imports Revolution.Modules.Attendance
Imports Xunit

Public Class AttendanceErrorMapTests
    <Theory>
    <InlineData(50001)>
    <InlineData(50003)>
    <InlineData(50004)>
    <InlineData(50005)>
    Public Sub RuleErrors_KeepTheDatabaseMessage(number As Integer)
        Assert.Equal("msg", AttendanceErrorMap.Translate(number, "msg"))
    End Sub

    <Fact>
    Public Sub NoCompanyInSession_IsAFault_NotARule()
        Assert.Null(AttendanceErrorMap.Translate(50002, "No company in session."))
    End Sub

    <Fact>
    Public Sub UnknownEmployee_ForeignKey_BecomesPlainMessage()
        Dim text = "The INSERT statement conflicted with the FOREIGN KEY constraint ""FK_AttendanceDaily_Employee""."
        Assert.Equal("Employee not found.", AttendanceErrorMap.Translate(547, text))
    End Sub

    <Fact>
    Public Sub OtherConstraintConflicts_AreFaults()
        Assert.Null(AttendanceErrorMap.Translate(547, "conflicted with the CHECK constraint ""CK_Other"""))
        Assert.Null(AttendanceErrorMap.Translate(1205, "deadlock"))
    End Sub
End Class
