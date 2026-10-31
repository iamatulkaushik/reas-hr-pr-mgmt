''' <summary>
''' What the database knows about one separation (pay.usp_Fnf_Facts), before any rule or calculator is applied. The Final* values come from the latest run that covers the last working
''' day and is at least Processed; FinalRunId is Nothing when no run covers it. Salary is the structure in force on the last working day. BonusMonths are from locked or paid runs of the
''' accounting year, the final run left out. Money is rupees as stored.
''' </summary>
Public NotInheritable Class FnfSourceFacts
    Public Property EmployeeId As Integer
    Public Property JoinDate As Date
    Public Property DateOfBirth As Date
    Public Property EmploymentTypeId As Integer
    Public Property ReasonId As Integer
    Public Property NoticeDate As Date?
    Public Property LastWorkingDay As Date
    Public Property NoticePeriodDays As Integer
    Public Property DailyWageDivisor As Decimal
    Public Property MonthlyGross As Decimal
    Public Property MonthlyBasicPlusDa As Decimal
    Public Property LoanOutstanding As Decimal
    Public Property BonusMonths As IReadOnlyList(Of FnfWorkedMonth) = New List(Of FnfWorkedMonth)()
    Public Property FinalRunId As Integer?
    Public Property FinalMonthKey As Integer
    Public Property FinalNetPay As Decimal
    Public Property FinalGross As Decimal
    Public Property FinalBasicPlusDa As Decimal
    Public Property FinalHra As Decimal
    Public Property FinalPaidDays As Decimal
    Public Property FinalTdsAmount As Decimal
End Class
