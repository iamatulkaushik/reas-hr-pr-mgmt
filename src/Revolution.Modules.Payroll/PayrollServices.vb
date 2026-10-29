Imports Revolution.Core

''' <summary>The services of the Payroll module that screens use. Grows as screens ship.</summary>
Public NotInheritable Class PayrollServices
    Public Sub New(salary As SalaryService, runs As PayrollRunService, payslips As PayslipService, loans As LoanService, tds As TdsDeclarationService, tdsProjection As TdsProjectionService, files As PayrollFileService, rates As IStatutoryRateService, exporter As ITableExporter, bankWriters As IReadOnlyList(Of IBankFileWriter),
                   leave As LeaveService, fnf As FnfService)
        If salary Is Nothing Then Throw New ArgumentNullException(NameOf(salary))
        If runs Is Nothing Then Throw New ArgumentNullException(NameOf(runs))
        If payslips Is Nothing Then Throw New ArgumentNullException(NameOf(payslips))
        If loans Is Nothing Then Throw New ArgumentNullException(NameOf(loans))
        If tds Is Nothing Then Throw New ArgumentNullException(NameOf(tds))
        If tdsProjection Is Nothing Then Throw New ArgumentNullException(NameOf(tdsProjection))
        If files Is Nothing Then Throw New ArgumentNullException(NameOf(files))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        If exporter Is Nothing Then Throw New ArgumentNullException(NameOf(exporter))
        If bankWriters Is Nothing OrElse bankWriters.Count = 0 Then Throw New ArgumentException("At least one bank file writer is needed.", NameOf(bankWriters))
        If leave Is Nothing Then Throw New ArgumentNullException(NameOf(leave))
        If fnf Is Nothing Then Throw New ArgumentNullException(NameOf(fnf))
        Me.Salary = salary
        Me.Runs = runs
        Me.Payslips = payslips
        Me.Loans = loans
        Me.Tds = tds
        Me.TdsProjection = tdsProjection
        Me.Files = files
        Me.Rates = rates
        Me.Exporter = exporter
        Me.BankWriters = bankWriters
        Me.Leave = leave
        Me.Fnf = fnf
    End Sub

    ''' <summary>Company pay policy, salary components and effective-dated employee salary.</summary>
    Public ReadOnly Property Salary As SalaryService

    ''' <summary>Create, process, submit, approve, lock, reopen and pay a payroll run.</summary>
    Public ReadOnly Property Runs As PayrollRunService

    ''' <summary>Payslip data for the report helper: masked ids by default, full ids for an approved run with Export on Payroll.</summary>
    Public ReadOnly Property Payslips As PayslipService

    ''' <summary>Loan types, sanction with schedule, cancel and list.</summary>
    Public ReadOnly Property Loans As LoanService

    ''' <summary>TDS declarations (Form 124): regime, deductions, HRA rent, previous employer.</summary>
    Public ReadOnly Property Tds As TdsDeclarationService

    ''' <summary>The year's tax and this month's TDS for one employee. The payroll run uses it (CR 0019) to work out each month's TDS.</summary>
    Public ReadOnly Property TdsProjection As TdsProjectionService

    ''' <summary>ECR, ESIC and bank file builders for an approved run.</summary>
    Public ReadOnly Property Files As PayrollFileService

    ''' <summary>Effective-dated rates. The ECR screen reads the EPF wage ceiling here.</summary>
    Public ReadOnly Property Rates As IStatutoryRateService

    ''' <summary>Writes the ESIC .xls (NPOI behind Core's ITableExporter).</summary>
    Public ReadOnly Property Exporter As ITableExporter

    ''' <summary>One writer per supported bank. State Bank of India first (Owner decision).</summary>
    Public ReadOnly Property BankWriters As IReadOnlyList(Of IBankFileWriter)

    ''' <summary>Leave types, the append-only leave ledger, balances and the encashment for an exit. Needs the FullAndFinal plan feature.</summary>
    Public ReadOnly Property Leave As LeaveService

    ''' <summary>Full and final settlement: save lines, approve, pay, reopen. Needs the FullAndFinal plan feature.</summary>
    Public ReadOnly Property Fnf As FnfService
End Class
