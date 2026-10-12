''' <summary>Checks placement fields against the database rules and returns every problem in plain words.</summary>
Public NotInheritable Class PlacementValidator
    Public Const MaximumNameLength As Integer = 120
    Public Const MaximumNationalityLength As Integer = 30
    Public Const MaximumEmailLength As Integer = 200

    Private Sub New()
    End Sub

    Public Shared Function Validate(placement As EmployeePlacement) As IReadOnlyList(Of String)
        If placement Is Nothing Then
            Throw New ArgumentNullException(NameOf(placement))
        End If
        Dim problems As New List(Of String)
        CheckClasses(placement, problems)
        CheckKeys(placement, problems)
        CheckText(placement, problems)
        Return problems
    End Function

    Private Shared Sub CheckClasses(p As EmployeePlacement, problems As List(Of String))
        Dim gender As String = If(p.Gender, String.Empty).Trim().ToUpperInvariant()
        If gender <> "M" AndAlso gender <> "F" AndAlso gender <> "O" Then
            problems.Add("Choose the gender.")
        End If
        If p.EmploymentTypeId < 1 OrElse p.EmploymentTypeId > 6 Then
            problems.Add("Choose the employment type.")
        End If
        If p.WorkerTypeId < 1 OrElse p.WorkerTypeId > 3 Then
            problems.Add("Choose worker, staff or manager.")
        End If
        If p.MaritalStatusId.HasValue AndAlso (p.MaritalStatusId.Value < 1 OrElse p.MaritalStatusId.Value > 4) Then
            problems.Add("Marital status is not valid.")
        End If
    End Sub

    Private Shared Sub CheckKeys(p As EmployeePlacement, problems As List(Of String))
        If p.FactoryId <= 0 Then
            problems.Add("Choose the factory.")
        End If
        If p.DepartmentId.HasValue AndAlso p.DepartmentId.Value <= 0 Then
            problems.Add("Department is not valid.")
        End If
        If p.BiometricId.HasValue AndAlso p.BiometricId.Value <= 0 Then
            problems.Add("Biometric id must be a positive number.")
        End If
    End Sub

    Private Shared Sub CheckText(p As EmployeePlacement, problems As List(Of String))
        If If(p.FatherOrSpouseName, String.Empty).Trim().Length > MaximumNameLength Then
            problems.Add("Father or spouse name is too long.")
        End If
        If If(p.Nationality, String.Empty).Trim().Length > MaximumNationalityLength Then
            problems.Add("Nationality is too long.")
        End If
        Dim email As String = If(p.Email, String.Empty).Trim()
        If email.Length > 0 AndAlso Not IsPlausibleEmail(email) Then
            problems.Add("Email address is not valid.")
        End If
    End Sub

    Private Shared Function IsPlausibleEmail(email As String) As Boolean
        If email.Length > MaximumEmailLength OrElse email.Contains(" ") Then
            Return False
        End If
        Dim at As Integer = email.IndexOf("@"c)
        Return at > 0 AndAlso at = email.LastIndexOf("@"c) AndAlso at < email.Length - 1
    End Function
End Class
