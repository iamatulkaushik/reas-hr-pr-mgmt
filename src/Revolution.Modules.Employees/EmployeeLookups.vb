''' <summary>Fixed code lists of mst.Employee (ADR 0003 codes). Names are for display only.</summary>
Public NotInheritable Class EmployeeLookups
    Private Sub New()
    End Sub

    Public Shared ReadOnly Genders As IReadOnlyList(Of KeyValuePair(Of String, String)) = New List(Of KeyValuePair(Of String, String)) From {
        New KeyValuePair(Of String, String)("M", "Male"),
        New KeyValuePair(Of String, String)("F", "Female"),
        New KeyValuePair(Of String, String)("O", "Other")}

    Public Shared ReadOnly EmploymentTypes As IReadOnlyList(Of KeyValuePair(Of Integer, String)) = Pairs("Permanent", "Contract", "Fixed term", "Casual", "Apprentice", "Trainee")

    Public Shared ReadOnly WorkerTypes As IReadOnlyList(Of KeyValuePair(Of Integer, String)) = Pairs("Worker", "Staff", "Manager")

    Public Shared ReadOnly MaritalStatuses As IReadOnlyList(Of KeyValuePair(Of Integer, String)) = Pairs("Single", "Married", "Widowed", "Divorced")

    Public Shared ReadOnly PayTypes As IReadOnlyList(Of KeyValuePair(Of Integer, String)) = New List(Of KeyValuePair(Of Integer, String)) From {
        New KeyValuePair(Of Integer, String)(CInt(PayType.DailyWage), "Daily wage"),
        New KeyValuePair(Of Integer, String)(CInt(PayType.MonthlySalary), "Monthly salary")}

    Public Shared ReadOnly PaymentModes As IReadOnlyList(Of KeyValuePair(Of Integer, String)) = New List(Of KeyValuePair(Of Integer, String)) From {
        New KeyValuePair(Of Integer, String)(CInt(PaymentMode.BankTransfer), "Bank transfer"),
        New KeyValuePair(Of Integer, String)(CInt(PaymentMode.Cash), "Cash"),
        New KeyValuePair(Of Integer, String)(CInt(PaymentMode.Cheque), "Cheque")}

    ''' <summary>Numbers the names 1, 2, 3 in the order given.</summary>
    Private Shared Function Pairs(ParamArray names As String()) As IReadOnlyList(Of KeyValuePair(Of Integer, String))
        Dim list As New List(Of KeyValuePair(Of Integer, String))()
        For i As Integer = 0 To names.Length - 1
            list.Add(New KeyValuePair(Of Integer, String)(i + 1, names(i)))
        Next
        Return list
    End Function
End Class
