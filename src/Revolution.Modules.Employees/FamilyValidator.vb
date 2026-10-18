''' <summary>Input rules for family members and nominations. Returns every problem found, empty when the input is fine.</summary>
Public NotInheritable Class FamilyValidator
    Public Const MaxNameLength As Integer = 120
    Public Const MaxRelationLength As Integer = 30
    Public Const MaxShare As Decimal = 100D
    Private Const ShareDecimals As Integer = 2

    Private Sub New()
    End Sub

    Public Shared Function Validate(member As FamilyMember, today As Date) As IReadOnlyList(Of String)
        Dim found As New List(Of String)()
        If String.IsNullOrWhiteSpace(member.Name) Then
            found.Add("Name is required.")
        ElseIf member.Name.Trim().Length > MaxNameLength Then
            found.Add($"Name can have at most {MaxNameLength} characters.")
        End If
        If String.IsNullOrWhiteSpace(member.Relation) Then
            found.Add("Relation is required.")
        ElseIf member.Relation.Trim().Length > MaxRelationLength Then
            found.Add($"Relation can have at most {MaxRelationLength} characters.")
        End If
        If member.DateOfBirth.HasValue AndAlso member.DateOfBirth.Value.Date > today.Date Then
            found.Add("Date of birth cannot be in the future.")
        End If
        Return found
    End Function

    Public Shared Function ValidateNomination(scheme As NomineeScheme, familyId As Integer, share As Decimal) As IReadOnlyList(Of String)
        Dim found As New List(Of String)()
        If Not [Enum].IsDefined(GetType(NomineeScheme), scheme) Then found.Add("Choose a scheme.")
        If familyId <= 0 Then found.Add("Choose a family member.")
        If share <= 0D OrElse share > MaxShare Then
            found.Add("Share must be above 0 and at most 100 percent.")
        ElseIf Decimal.Round(share, ShareDecimals) <> share Then
            found.Add("Share can have two decimal places at most.")
        End If
        Return found
    End Function
End Class
