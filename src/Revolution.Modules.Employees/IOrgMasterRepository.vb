''' <summary>Factory, department and designation storage through mst.* stored procedures (ADR 0008). Save returns the row id.</summary>
Public Interface IOrgMasterRepository
    Function ListBanks() As IReadOnlyList(Of BankItem)

    Function ListStates() As IReadOnlyList(Of StateItem)

    Function ListFactories(activeOnly As Boolean) As IReadOnlyList(Of FactoryItem)

    Function SaveFactory(item As FactoryItem) As Integer

    Function ListDepartments(factoryId As Integer?, activeOnly As Boolean) As IReadOnlyList(Of DepartmentItem)

    Function SaveDepartment(item As DepartmentItem) As Integer

    Function ListDesignations(activeOnly As Boolean) As IReadOnlyList(Of DesignationItem)

    Function SaveDesignation(item As DesignationItem) As Integer
End Interface
