''' <summary>A family member of an employee. Nominees are chosen from these.</summary>
Public NotInheritable Class FamilyMember
    Public Property FamilyId As Integer
    Public Property Name As String = String.Empty
    Public Property Relation As String = String.Empty
    Public Property DateOfBirth As Date?
    Public Property IsDependent As Boolean
    Public Property IsResiding As Boolean = True
End Class
