Imports Revolution.Core

''' <summary>Storage for TDS declarations (pay.usp_TdsDeclaration_*, usp_TdsDeclarationItem_*, usp_TdsHraRent_*, usp_TdsPrevEmployer_*). Rule errors come as PayrollRuleException.</summary>
Public Interface ITdsRepository
    ''' <summary>Nothing when the employee has no declaration for the year.</summary>
    Function GetDeclaration(employeeId As Integer, financialYear As Integer) As TdsDeclarationHeader
    ''' <summary>Adds the declaration or changes its regime. Refused when Locked. Returns the declaration id.</summary>
    Function SaveDeclaration(employeeId As Integer, financialYear As Integer, regime As TdsRegime, userId As Integer?) As Integer
    Sub SetStatus(declarationId As Integer, status As TdsDeclarationStatus, userId As Integer?)
    Function ListItems(declarationId As Integer) As IReadOnlyList(Of TdsItem)
    ''' <summary>Replaces every item in one transaction. Items are numbered from 1 in the order given.</summary>
    Sub ReplaceItems(declarationId As Integer, items As IReadOnlyList(Of TdsItem), userId As Integer?)
    Function ListRents(declarationId As Integer) As IReadOnlyList(Of TdsRentView)
    Sub ReplaceRents(declarationId As Integer, rents As IReadOnlyList(Of TdsRentWrite), userId As Integer?)
    ''' <summary>Nothing when there is no previous employer.</summary>
    Function GetPrevEmployer(declarationId As Integer) As TdsPrevEmployer
    Sub SavePrevEmployer(declarationId As Integer, prev As TdsPrevEmployer, userId As Integer?)
    Sub ClearPrevEmployer(declarationId As Integer, userId As Integer?)
End Interface
