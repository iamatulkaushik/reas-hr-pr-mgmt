''' <summary>Family members and nominations through mst.usp_EmployeeFamily_* and mst.usp_EmployeeNominee_* (ADR 0008).</summary>
Public Interface IEmployeeFamilyRepository
    Function ListFamily(employeeId As Integer) As IReadOnlyList(Of FamilyMember)

    ''' <summary>Adds the member (FamilyId 0) or changes the saved one. Returns the family id.</summary>
    Function SaveFamily(employeeId As Integer, member As FamilyMember) As Integer

    Sub DeleteFamily(familyId As Integer)

    Function ListNominees(employeeId As Integer) As IReadOnlyList(Of NomineeRow)

    ''' <summary>Adds the nomination or changes its share.</summary>
    Sub SaveNominee(employeeId As Integer, scheme As NomineeScheme, familyId As Integer, sharePercent As Decimal)

    Sub DeleteNominee(nomineeId As Integer)
End Interface
