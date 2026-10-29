Imports Revolution.Core

''' <summary>
''' Everything the planner needs to build one employee's settlement, already read from the database or typed by the Owner. Money is whole rupees. Leave a rule as Nothing to skip
''' that line (no bonus rule row, no gratuity rule row). The rules are the verified rows read for the exit date; checking they are verified is the caller's job, as everywhere else.
''' </summary>
Public NotInheritable Class FnfFacts
    Public Property EmployeeId As Integer
    Public Property JoinDate As Date
    Public Property LastWorkingDay As Date

    ''' <summary>Net pay for the final month up to the exit date (calendar days), from the run that covers it or typed.</summary>
    Public Property LastSalaryNet As Decimal

    ''' <summary>Gross wage for one day: gross divided by the pay-cycle divisor for a monthly employee, the daily rate for a daily wager.</summary>
    Public Property DailyWageGross As Decimal

    ''' <summary>True when the employee resigned. Notice pay is recovered only then.</summary>
    Public Property NoticeApplies As Boolean
    Public Property NoticeDate As Date?
    Public Property NoticePeriodDays As Integer

    ''' <summary>Permanent and fixed term only (Owner, 2026-10-27). Contract, casual, apprentice and trainee are not covered.</summary>
    Public Property GratuityCovered As Boolean
    Public Property IsFixedTerm As Boolean
    ''' <summary>Death or disablement: the minimum service does not apply.</summary>
    Public Property WaiveGratuityMinimum As Boolean
    ''' <summary>Last drawn monthly Basic plus DA.</summary>
    Public Property GratuityWage As Decimal
    Public Property GratuityRule As GratuityRule

    Public Property BonusRule As BonusRule
    Public Property BonusLastMonthlyWage As Decimal
    Public Property BonusMonthlyWages As IReadOnlyList(Of Decimal) = New List(Of Decimal)()
    Public Property BonusDaysWorked As Integer
    ''' <summary>Chosen by the Owner inside the rule's range. Ignored when there is no bonus rule.</summary>
    Public Property BonusPct As Decimal
    Public Property BonusAlreadyPaid As Decimal

    Public Property LeaveEncashable As Boolean
    Public Property LeaveBalanceDays As Decimal
    ''' <summary>The leave type's encashment limit in days, zero for no limit.</summary>
    Public Property LeaveMaxEncashDays As Decimal

    Public Property LoanOutstanding As Decimal
    ''' <summary>Final tax true-up from FnfTdsTrueUp.Adjustment: positive is a shortfall, negative a refund.</summary>
    Public Property TdsAdjustment As Decimal
    ''' <summary>The Owner's option on the deduction side. Zero unless the Owner chooses to recover arrears.</summary>
    Public Property ArrearsRecovery As Decimal
End Class
