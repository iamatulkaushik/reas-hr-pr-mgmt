''' <summary>Storage for the bonus run (pay.usp_Bonus_Facts, usp_BonusRun_Register, usp_ArrearsRun_NextNo).</summary>
Public Interface IBonusSource
    ''' <summary>The months and the bonus already paid for the accounting year that starts on the given 1st of April.</summary>
    Function Load(yearFrom As Date) As BonusFacts

    ''' <summary>The next free run number for the pay period, 2 or more.</summary>
    Function NextRunNo(period As RunPeriod) As Integer

    ''' <summary>Marks an off-cycle run as the bonus run of the accounting year, with the percentage chosen.</summary>
    Sub Register(runId As Integer, yearFrom As Date, pct As Decimal, userId As Integer?)
End Interface
