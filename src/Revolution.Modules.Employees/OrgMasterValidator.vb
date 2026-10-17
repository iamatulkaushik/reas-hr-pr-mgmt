Imports System.Text.RegularExpressions

''' <summary>Input rules for factory, department and designation. Returns every problem found, empty when the item is fine.</summary>
Public NotInheritable Class OrgMasterValidator
    Public Const MaxCodeLength As Integer = 10
    Public Const MaxShortName As Integer = 100
    Public Const MaxFactoryName As Integer = 150
    Public Const MinCategory As Integer = 1
    Public Const MaxCategory As Integer = 7
    Private Const MaxStateId As Integer = 255
    Private Const MaxAddress As Integer = 150
    Private Const MaxCity As Integer = 100
    Private Const MaxLicence As Integer = 50
    Private Const MaxManager As Integer = 120
    Private Const MaxEpfSub As Integer = 20
    Private Const MaxEsiSub As Integer = 17

    Private Shared ReadOnly CodePattern As New Regex("^[A-Za-z0-9_-]+$", RegexOptions.Compiled)

    Private Sub New()
    End Sub

    Public Shared Function Validate(item As FactoryItem) As IReadOnlyList(Of String)
        Dim found As New List(Of String)()
        CheckCode(found, item.Code)
        CheckName(found, item.Name, MaxFactoryName, "Factory name")
        If item.StateId <= 0 OrElse item.StateId > MaxStateId Then found.Add("Choose the state of the factory.")
        If item.MaxWorkers.HasValue AndAlso item.MaxWorkers.Value <= 0 Then found.Add("Maximum workers must be above zero.")
        CheckLength(found, item.AddressLine1, MaxAddress, "Address line 1")
        CheckLength(found, item.AddressLine2, MaxAddress, "Address line 2")
        CheckLength(found, item.City, MaxCity, "City")
        CheckLength(found, item.LicenceNo, MaxLicence, "Factory licence number")
        CheckLength(found, item.ManagerName, MaxManager, "Manager name")
        CheckLength(found, item.EpfSubCode, MaxEpfSub, "EPF sub code")
        CheckLength(found, item.EsiSubCode, MaxEsiSub, "ESI sub code")
        Return found
    End Function

    Public Shared Function Validate(item As DepartmentItem) As IReadOnlyList(Of String)
        Dim found As New List(Of String)()
        CheckCode(found, item.Code)
        CheckName(found, item.Name, MaxShortName, "Department name")
        If item.FactoryId <= 0 Then found.Add("Choose the factory of the department.")
        Return found
    End Function

    Public Shared Function Validate(item As DesignationItem) As IReadOnlyList(Of String)
        Dim found As New List(Of String)()
        CheckCode(found, item.Code)
        CheckName(found, item.Name, MaxShortName, "Designation name")
        If item.CategoryId < MinCategory OrElse item.CategoryId > MaxCategory Then
            found.Add("Choose a skill category.")
        End If
        Return found
    End Function

    Private Shared Sub CheckCode(found As List(Of String), code As String)
        If String.IsNullOrWhiteSpace(code) Then
            found.Add("Code is required.")
        ElseIf code.Trim().Length > MaxCodeLength Then
            found.Add($"Code can have at most {MaxCodeLength} characters.")
        ElseIf Not CodePattern.IsMatch(code.Trim()) Then
            found.Add("Code can have letters, digits, dash and underscore only.")
        End If
    End Sub

    Private Shared Sub CheckName(found As List(Of String), name As String, maxLength As Integer, caption As String)
        If String.IsNullOrWhiteSpace(name) Then
            found.Add($"{caption} is required.")
        ElseIf name.Trim().Length > maxLength Then
            found.Add($"{caption} can have at most {maxLength} characters.")
        End If
    End Sub

    Private Shared Sub CheckLength(found As List(Of String), value As String, maxLength As Integer, caption As String)
        If value IsNot Nothing AndAlso value.Trim().Length > maxLength Then
            found.Add($"{caption} can have at most {maxLength} characters.")
        End If
    End Sub
End Class
