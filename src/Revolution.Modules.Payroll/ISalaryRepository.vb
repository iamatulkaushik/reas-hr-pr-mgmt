''' <summary>Storage for company pay policy, salary components and effective-dated employee salary (pay.usp_Policy_*, usp_Component_*, usp_Salary_*).</summary>
Public Interface ISalaryRepository
    Function GetPolicy() As PolicySettings
    Sub SavePolicy(policy As PolicySettings, userId As Integer?)
    Function ListComponents() As IReadOnlyList(Of SalaryComponentItem)
    ''' <summary>Adds the component when ComponentId is Nothing, otherwise changes it. Returns the id.</summary>
    Function SaveComponent(component As SalaryComponentItem) As Integer
    ''' <summary>The salary in force on the date, or Nothing when the employee has none yet.</summary>
    Function GetSalaryAt(employeeId As Integer, asOf As Date) As EmployeeSalaryView
    ''' <summary>Adds a salary revision as a new header. Returns its id.</summary>
    Function SaveSalary(employeeId As Integer, effectiveFrom As Date, reason As String, lines As IReadOnlyList(Of SalaryLineWrite), userId As Integer?) As Integer
End Interface
