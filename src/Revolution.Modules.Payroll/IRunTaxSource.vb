''' <summary>Reads the tax facts for every employee of a run in one go (pay.usp_RunTax_Facts, and the declaration through the TDS repository). The run being processed is left out.</summary>
Public Interface IRunTaxSource
    ''' <summary>One entry per employee id asked for. An employee with no earlier locked pay gets an entry with no months and no tax deducted.</summary>
    Function Load(runId As Integer, financialYear As Integer, employeeIds As IReadOnlyList(Of Integer)) As IReadOnlyDictionary(Of Integer, EmployeeTaxFacts)
End Interface
