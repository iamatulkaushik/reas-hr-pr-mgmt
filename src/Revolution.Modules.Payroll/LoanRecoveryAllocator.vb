''' <summary>
''' Scheduled EMI and how a recovered amount is split over open installments.
''' Scheduled EMI for a run is everything outstanding with a due month up to the month of the period end (deferred EMI is included).
''' Recovery goes to the oldest due month first, then lower loan number, then lower installment number.
''' </summary>
Public NotInheritable Class LoanRecoveryAllocator
    Private Sub New()
    End Sub

    Public Shared Function ScheduledEmi(open As IEnumerable(Of OpenInstallment), periodMonthKey As Integer) As Decimal
        If open Is Nothing Then Throw New ArgumentNullException(NameOf(open))
        Return open.Where(Function(o) o.DueMonthKey <= periodMonthKey).Sum(Function(o) o.Outstanding)
    End Function

    Public Shared Function Allocate(emiTaken As Decimal, open As IEnumerable(Of OpenInstallment), periodMonthKey As Integer) As IReadOnlyList(Of RecoveryAllocation)
        If open Is Nothing Then Throw New ArgumentNullException(NameOf(open))
        If emiTaken < 0D Then Throw New ArgumentOutOfRangeException(NameOf(emiTaken), "Amount cannot be negative.")
        Dim due As List(Of OpenInstallment) = open.Where(Function(o) o.DueMonthKey <= periodMonthKey).
            OrderBy(Function(o) o.DueMonthKey).ThenBy(Function(o) o.LoanID).ThenBy(Function(o) o.InstallmentNo).ToList()
        If emiTaken > due.Sum(Function(o) o.Outstanding) Then Throw New ArgumentOutOfRangeException(NameOf(emiTaken), "More than is outstanding.")
        Dim result As New List(Of RecoveryAllocation)
        Dim left As Decimal = emiTaken
        For Each o As OpenInstallment In due
            If left <= 0D Then Exit For
            Dim part As Decimal = Math.Min(left, o.Outstanding)
            result.Add(New RecoveryAllocation(o.LoanID, o.InstallmentNo, part))
            left -= part
        Next
        Return result
    End Function
End Class
