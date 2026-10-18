''' <summary>One nomination: a family member named for a scheme with a percentage share.</summary>
Public NotInheritable Class NomineeRow
    Public Property NomineeId As Integer
    Public Property Scheme As NomineeScheme
    Public Property FamilyId As Integer
    Public Property MemberName As String = String.Empty
    Public Property Relation As String = String.Empty
    Public Property SharePercent As Decimal
End Class
