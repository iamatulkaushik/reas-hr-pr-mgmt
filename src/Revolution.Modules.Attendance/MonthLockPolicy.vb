Imports Revolution.Core

''' <summary>Rules for closing a month, editing a closed month, and reopening it. Who may reopen is a permission, not decided here.</summary>
Public NotInheritable Class MonthLockPolicy
    Public Const MinimumReasonLength As Integer = 10
    Private Const KeyFactor As Integer = 100

    Private Sub New()
    End Sub

    ''' <summary>Key for a month, for example 202610. Used in the set of locked months.</summary>
    Public Shared Function KeyOf(day As Date) As Integer
        Return day.Year * KeyFactor + day.Month
    End Function

    Public Shared Function IsLocked(day As Date, lockedMonths As ISet(Of Integer)) As Boolean
        If lockedMonths Is Nothing Then
            Throw New ArgumentNullException(NameOf(lockedMonths))
        End If
        Return lockedMonths.Contains(KeyOf(day))
    End Function

    ''' <summary>A month can close only after it has ended and every day is marked.</summary>
    Public Shared Function CanClose(summary As MonthSummary, today As Date) As OperationResult
        If summary Is Nothing Then
            Throw New ArgumentNullException(NameOf(summary))
        End If
        Dim lastDay As New Date(summary.Year, summary.Month, summary.DaysInMonth)
        If today.Date <= lastDay Then
            Return OperationResult.Fail("The month has not ended yet.")
        End If
        If summary.UnmarkedDays > 0 Then
            Return OperationResult.Fail($"{summary.UnmarkedDays} day(s) are not marked.")
        End If
        Return OperationResult.Ok()
    End Function

    Public Shared Function ValidateReopenReason(reason As String) As OperationResult
        If String.IsNullOrWhiteSpace(reason) OrElse reason.Trim().Length < MinimumReasonLength Then
            Return OperationResult.Fail($"Give a reason of at least {MinimumReasonLength} characters.")
        End If
        Return OperationResult.Ok()
    End Function
End Class
