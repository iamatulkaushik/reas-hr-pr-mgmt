''' <summary>
''' Which status changes a payroll run may make. This is the same table as pay.usp_Run_ChangeStatus. The procedure is the final check.
''' Draft to Processed is not here: only processing the run does it. Approval is skipped (Processed to Approved) only when the plan needs no approval.
''' </summary>
Public NotInheritable Class RunStateMachine
    Private Sub New()
    End Sub

    Private Shared ReadOnly Allowed As (From As PayrollRunStatus, [To] As PayrollRunStatus)() = {
        (PayrollRunStatus.Processed, PayrollRunStatus.Draft),
        (PayrollRunStatus.Processed, PayrollRunStatus.Submitted),
        (PayrollRunStatus.Processed, PayrollRunStatus.Approved),
        (PayrollRunStatus.Submitted, PayrollRunStatus.Approved),
        (PayrollRunStatus.Submitted, PayrollRunStatus.Draft),
        (PayrollRunStatus.Approved, PayrollRunStatus.Locked),
        (PayrollRunStatus.Locked, PayrollRunStatus.Paid),
        (PayrollRunStatus.Locked, PayrollRunStatus.Draft)
    }

    Public Shared Function CanChange(fromStatus As PayrollRunStatus, toStatus As PayrollRunStatus) As Boolean
        Return Allowed.Any(Function(a) a.From = fromStatus AndAlso a.To = toStatus)
    End Function

    ''' <summary>Reopening a locked run needs a written reason of at least 10 characters.</summary>
    Public Shared Function NeedsReason(fromStatus As PayrollRunStatus, toStatus As PayrollRunStatus) As Boolean
        Return fromStatus = PayrollRunStatus.Locked AndAlso toStatus = PayrollRunStatus.Draft
    End Function

    ''' <summary>Lines can change only while the run is Draft or Processed.</summary>
    Public Shared Function CanEditLines(status As PayrollRunStatus) As Boolean
        Return status = PayrollRunStatus.Draft OrElse status = PayrollRunStatus.Processed
    End Function
End Class
