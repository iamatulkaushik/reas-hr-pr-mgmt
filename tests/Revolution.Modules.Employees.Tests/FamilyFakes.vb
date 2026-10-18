Imports Revolution.Core
Imports Revolution.Modules.Employees

Friend NotInheritable Class FakeEmployeePermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return moduleKey = ModuleKey.Employees AndAlso Allowed.Contains(action)
    End Function
End Class

Friend NotInheritable Class FakeFamilyRepository
    Implements IEmployeeFamilyRepository

    Public ReadOnly Family As New List(Of FamilyMember)()
    Public ReadOnly Nominees As New List(Of NomineeRow)()
    Public RuleError As String
    Private _nextId As Integer = 100

    Public Function ListFamily(employeeId As Integer) As IReadOnlyList(Of FamilyMember) Implements IEmployeeFamilyRepository.ListFamily
        Return Family
    End Function

    Public Function SaveFamily(employeeId As Integer, member As FamilyMember) As Integer Implements IEmployeeFamilyRepository.SaveFamily
        ThrowIfRule()
        If member.FamilyId = 0 Then
            _nextId += 1
            member.FamilyId = _nextId
        End If
        Family.Add(member)
        Return member.FamilyId
    End Function

    Public Sub DeleteFamily(familyId As Integer) Implements IEmployeeFamilyRepository.DeleteFamily
        ThrowIfRule()
        Family.RemoveAll(Function(f) f.FamilyId = familyId)
    End Sub

    Public Function ListNominees(employeeId As Integer) As IReadOnlyList(Of NomineeRow) Implements IEmployeeFamilyRepository.ListNominees
        Return Nominees
    End Function

    Public Sub SaveNominee(employeeId As Integer, scheme As NomineeScheme, familyId As Integer, sharePercent As Decimal) Implements IEmployeeFamilyRepository.SaveNominee
        ThrowIfRule()
        Nominees.Add(New NomineeRow With {.Scheme = scheme, .FamilyId = familyId, .SharePercent = sharePercent})
    End Sub

    Public Sub DeleteNominee(nomineeId As Integer) Implements IEmployeeFamilyRepository.DeleteNominee
        ThrowIfRule()
        Nominees.RemoveAll(Function(n) n.NomineeId = nomineeId)
    End Sub

    Private Sub ThrowIfRule()
        If RuleError IsNot Nothing Then Throw New EmployeeRuleException(RuleError)
    End Sub
End Class
