Imports Revolution.Modules.Payroll

''' <summary>Finds the regular run (run 1) that paid a pay period, through pay.usp_ArrearsRun_Find. Nothing when no run was made for it.</summary>
Public Interface IArrearsSource
    Function FindRegularRun(period As RunPeriod) As PayrollRunHeader
    ''' <summary>The next free run number for the pay period, 2 or more (pay.usp_ArrearsRun_NextNo).</summary>
    Function NextRunNo(period As RunPeriod) As Integer
End Interface
