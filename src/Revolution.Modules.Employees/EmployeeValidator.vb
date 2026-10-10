''' <summary>Checks an employee draft and returns every problem in plain words. Empty list means valid.</summary>
Public NotInheritable Class EmployeeValidator
    Public Const MinimumEmploymentAge As Integer = 14
    Public Const MaximumNameLength As Integer = 100

    Private Sub New()
    End Sub

    Public Shared Function Validate(draft As EmployeeDraft, today As Date) As IReadOnlyList(Of String)
        If draft Is Nothing Then
            Throw New ArgumentNullException(NameOf(draft))
        End If
        Dim problems As New List(Of String)
        CheckIdentity(draft, today, problems)
        CheckGovernmentIds(draft, problems)
        CheckBank(draft, problems)
        Return problems
    End Function

    Private Shared Sub CheckIdentity(draft As EmployeeDraft, today As Date, problems As List(Of String))
        Dim name As String = If(draft.FullName, String.Empty).Trim()
        If name.Length = 0 Then
            problems.Add("Enter the employee name.")
        ElseIf name.Length > MaximumNameLength Then
            problems.Add("Name is too long.")
        End If
        If Not [Enum].IsDefined(GetType(PayType), draft.PayType) Then
            problems.Add("Choose daily wage or monthly salary.")
        End If
        CheckDates(draft, today, problems)
    End Sub

    Private Shared Sub CheckDates(draft As EmployeeDraft, today As Date, problems As List(Of String))
        If draft.DateOfBirth = Date.MinValue Then
            problems.Add("Enter the date of birth.")
        ElseIf draft.DateOfBirth >= today Then
            problems.Add("Date of birth must be in the past.")
        End If
        If draft.JoinDate = Date.MinValue Then
            problems.Add("Enter the joining date.")
        ElseIf draft.DateOfBirth <> Date.MinValue AndAlso draft.JoinDate < draft.DateOfBirth.AddYears(MinimumEmploymentAge) Then
            problems.Add("Employee must be at least 14 years old on joining.")
        End If
    End Sub

    Private Shared Sub CheckGovernmentIds(draft As EmployeeDraft, problems As List(Of String))
        CheckOptional(draft.Mobile, AddressOf MobileValidator.IsValid, "Mobile must be 10 digits starting 6 to 9.", problems)
        CheckOptional(draft.Pan, AddressOf PanValidator.IsValid, "PAN format is wrong.", problems)
        CheckOptional(draft.Aadhaar, AddressOf AadhaarValidator.IsValid, "Aadhaar number is not valid.", problems)
        CheckOptional(draft.Uan, AddressOf UanValidator.IsValid, "UAN must be 12 digits.", problems)
        CheckOptional(draft.EsiIp, AddressOf EsiIpValidator.IsValid, "ESI IP number must be 10 digits.", problems)
    End Sub

    Private Shared Sub CheckBank(draft As EmployeeDraft, problems As List(Of String))
        Dim hasAccount As Boolean = Not String.IsNullOrWhiteSpace(draft.BankAccount)
        Dim hasIfsc As Boolean = Not String.IsNullOrWhiteSpace(draft.Ifsc)
        If hasAccount Xor hasIfsc Then
            problems.Add("Enter both bank account and IFSC, or leave both blank.")
            Return
        End If
        CheckOptional(draft.BankAccount, AddressOf BankAccountValidator.IsValid, "Bank account must be 9 to 18 digits.", problems)
        CheckOptional(draft.Ifsc, AddressOf IfscValidator.IsValid, "IFSC format is wrong.", problems)
    End Sub

    Private Shared Sub CheckOptional(value As String, isValid As Func(Of String, Boolean), message As String, problems As List(Of String))
        If String.IsNullOrWhiteSpace(value) Then
            Return
        End If
        If Not isValid(value) Then
            problems.Add(message)
        End If
    End Sub
End Class
