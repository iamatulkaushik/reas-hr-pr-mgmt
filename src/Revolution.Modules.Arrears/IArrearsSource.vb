Imports Revolution.Modules.Payroll

''' <summary>Finds the regular run (run 1) that paid a pay period, through pay.usp_ArrearsRun_Find. Nothing when no run was made for it.</summary>
Public Interface IArrearsSource
    Function FindRegularRun(period As RunPeriod) As PayrollRunHeader
End Interface
