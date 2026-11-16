''' <summary>
''' The Monthly Pack of the open company (ADR 0027): which statutory items fall due for a month, where each stands, one click to write the files, and the filing and payment
''' references the Associate keeps. Needs View on Statutory; Generate and Mark need Create or Edit. The figures are those of the approved or locked payroll only.
''' </summary>
Public Interface IMonthlyPackService
    ''' <summary>The lines for the month (the first of the month; any date in the month is accepted).</summary>
    Function GetPack(periodStart As Date) As OperationResult(Of MonthlyPack)

    ''' <summary>Writes the file or summary of one line into the folder and returns the file name. Marks the line Generated.</summary>
    Function Generate(periodStart As Date, line As MonthlyPackLine, folder As String) As OperationResult(Of String)

    ''' <summary>Writes every line that is Ready or Changed, and the Monthly Pack summary workbook, into the folder; returns the file names.</summary>
    Function GenerateAll(periodStart As Date, folder As String) As OperationResult(Of IReadOnlyList(Of String))

    ''' <summary>Records that a line was Filed or Paid, with the reference (for example the acknowledgement or challan number) and the date.</summary>
    Function Mark(periodStart As Date, line As MonthlyPackLine, status As PackStatus, reference As String, onDate As Date) As OperationResult
End Interface
