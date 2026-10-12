''' <summary>Statutory flag storage through mst.usp_EmployeeStatutory_Get and SaveFlags. Ids are handled by IEmployeeRepository.</summary>
Public Interface IStatutoryFlagsRepository
    ''' <summary>Nothing when the employee has no statutory row yet.</summary>
    Function GetFlags(employeeId As Integer) As StatutoryFlags

    Sub SaveFlags(employeeId As Integer, flags As StatutoryFlags, userId As Integer?)
End Interface
