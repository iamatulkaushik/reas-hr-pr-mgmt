''' <summary>Storage for payroll runs through the pay.usp_Run*, usp_RunInput_List, usp_LoanInstallment_ListOpen and usp_Recovery_* procedures.</summary>
Public Interface IPayrollRunRepository
    ''' <summary>Creates a Draft run and returns its id.</summary>
    Function CreateRun(period As RunPeriod, runNo As Integer, userId As Integer?) As Integer
    ''' <summary>The run, or Nothing when it does not exist for this company.</summary>
    Function GetRun(runId As Integer) As PayrollRunHeader
    Function ListLines(runId As Integer) As IReadOnlyList(Of RunLineResult)
    Function ListEvents(runId As Integer) As IReadOnlyList(Of PayrollRunEventItem)
    ''' <summary>One facts row per employee working in the period, with attendance, salary in force on the period end, carry-in and open installments.</summary>
    Function LoadInputs(period As RunPeriod) As IReadOnlyList(Of RunEmployeeFacts)
    ''' <summary>Replaces all lines and earnings of a Draft or Processed run and sets it Processed.</summary>
    Sub ReplaceLines(runId As Integer, lines As IReadOnlyList(Of RunLineResult), userId As Integer?)
    ''' <summary>Changes the status. The database checks the table, the checksum and the maker-checker rule, and reads the user from the session.</summary>
    Sub ChangeStatus(runId As Integer, toStatus As PayrollRunStatus, action As String, reason As String)
    ''' <summary>Open installments with a due month up to the month key, for every employee.</summary>
    Function ListOpenInstallments(uptoMonthKey As Integer) As IReadOnlyList(Of EmployeeInstallment)
    Sub SaveRecoveries(runId As Integer, recoveries As IReadOnlyList(Of RecoveryAllocation))
    Sub ReverseRecoveries(runId As Integer)
End Interface
