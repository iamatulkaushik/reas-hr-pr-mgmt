''' <summary>Reads payslip data through rpt.usp_Payslip_Get (read only). Never reads a ciphertext column.</summary>
Public Interface IPayslipRepository
    ''' <summary>One row per employee of the run, or only the employee given. Throws PayrollRuleException when the run is not in this company.</summary>
    Function GetPayslipRows(runId As Integer, employeeId As Integer?) As IReadOnlyList(Of PayslipRow)
End Interface
