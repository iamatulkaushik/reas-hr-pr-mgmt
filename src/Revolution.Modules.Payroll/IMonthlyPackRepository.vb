Imports Revolution.Core

''' <summary>Storage for the Monthly Pack (ADR 0027): the month's figures, the filing records and the due dates. Rule breaks raise PackRuleException.</summary>
Public Interface IMonthlyPackRepository
    Function GetFigures(periodStart As Date) As PackFigures

    ''' <summary>The states of the company that have a labour welfare rule in force with the month among its due months.</summary>
    Function LwfStates(periodStart As Date) As IReadOnlyList(Of PackStateAmount)

    Function ListFilings(periodStart As Date) As IReadOnlyList(Of PackFiling)

    ''' <summary>Generated stores the amount and count and clears any earlier filing; Filed and Paid need an existing line and their date.</summary>
    Sub RecordFiling(periodStart As Date, kind As ComplianceKind, stateId As Integer?, status As PackStatus, amountPayable As Decimal?, employeeCount As Integer?,
                     filedOn As Date?, paidOn As Date?, referenceNo As String, userId As Integer?)

    Function ResolveDue(kind As ComplianceKind, stateId As Integer?, periodStart As Date) As PackDue
End Interface
